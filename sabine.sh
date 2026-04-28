#!/bin/bash
# ============================================================================
# Sabine - Server & Account Management Script
# ============================================================================
# Usage:
#   ./sabine.sh install                  - One-time setup (deps, DB, build, configs, admin)
#   ./sabine.sh start [auth|char|zone|web]
#   ./sabine.sh stop  [auth|char|zone|web]
#   ./sabine.sh restart
#   ./sabine.sh status                   - Running/stopped + CPU/MEM
#   ./sabine.sh logs  [auth|char|zone|web]
#   ./sabine.sh update                   - git pull + rebuild + restart if running
#   ./sabine.sh ip                       - Configure server IP for clients
#   ./sabine.sh version                  - Pick client/packet version (alpha, beta1, ...)
#   ./sabine.sh account create
#   ./sabine.sh account list
#   ./sabine.sh account admin <username>
#   ./sabine.sh help
# ============================================================================

cd "$(dirname "$0")" || exit 1

# ---------------------------------------------------------------------------
# Colors
# ---------------------------------------------------------------------------
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

info()    { echo -e "  ${BLUE}[INFO]${NC}    $1"; }
success() { echo -e "  ${GREEN}[OK]${NC}      $1"; }
warn()    { echo -e "  ${YELLOW}[WARN]${NC}    $1"; }
error()   { echo -e "  ${RED}[ERROR]${NC}   $1"; }
step()    { echo -e "\n${BOLD}${CYAN}── Step $1: $2 ──${NC}"; }

# ---------------------------------------------------------------------------
# Server table — drives start/stop/status
# Format: ScreenSuffix|DllName|FriendlyName
# ---------------------------------------------------------------------------
SERVERS=(
    "auth|AuthServer.dll|Auth"
    "char|CharServer.dll|Char"
    "zone|ZoneServer.dll|Zone"
    "web|WebServer.dll|Web"
)

SCREEN_PREFIX="sabine"
LOGS_DIR="logs"

# Locate build output. Prefer Release, fall back to Debug.
SUBFOLDER="net8.0"
BINPATH=""
detect_binpath() {
    if [ -d "bin/Release/$SUBFOLDER" ]; then
        BINPATH="bin/Release/$SUBFOLDER"
    elif [ -d "bin/Debug/$SUBFOLDER" ]; then
        BINPATH="bin/Debug/$SUBFOLDER"
    fi
}

require_binpath() {
    detect_binpath
    if [ -z "$BINPATH" ]; then
        error "No build found. Run 'dotnet build Sabine.sln -c Release' or './sabine.sh install' first."
        exit 1
    fi
}

# ---------------------------------------------------------------------------
# Config parsing — Sabine uses 'key: value' format (with // comments)
# ---------------------------------------------------------------------------
parse_conf() {
    # $1 = key, $2 = file
    grep -E "^[[:space:]]*${1}[[:space:]]*:" "$2" 2>/dev/null \
        | head -1 \
        | sed -E "s|^[[:space:]]*${1}[[:space:]]*:[[:space:]]*||;s|//.*$||" \
        | sed -E 's/[[:space:]]+$//'
}

read_db_config() {
    local sys="system/conf/database.conf"
    local usr="user/conf/database.conf"

    if [ ! -f "$sys" ]; then
        error "$sys not found. Are you running this from the repo root?"
        exit 1
    fi

    DB_HOST=$(parse_conf "db_host" "$sys")
    DB_PORT=$(parse_conf "db_port" "$sys")
    DB_USER=$(parse_conf "db_user" "$sys")
    DB_PASS=$(parse_conf "db_pass" "$sys")
    DB_NAME=$(parse_conf "db_name" "$sys")

    # User overrides take precedence
    if [ -f "$usr" ]; then
        local v
        v=$(parse_conf "db_host" "$usr"); [ -n "$v" ] && DB_HOST="$v"
        v=$(parse_conf "db_port" "$usr"); [ -n "$v" ] && DB_PORT="$v"
        v=$(parse_conf "db_user" "$usr"); [ -n "$v" ] && DB_USER="$v"
        v=$(parse_conf "db_pass" "$usr"); [ -n "$v" ] && DB_PASS="$v"
        v=$(parse_conf "db_name" "$usr"); [ -n "$v" ] && DB_NAME="$v"
    fi

    DB_HOST="${DB_HOST:-127.0.0.1}"
    DB_PORT="${DB_PORT:-3306}"
    DB_NAME="${DB_NAME:-sabine}"
    DB_USER="${DB_USER:-root}"
}

# Pick mariadb or mysql client
mysql_client() {
    if command -v mariadb &>/dev/null; then echo "mariadb"
    elif command -v mysql &>/dev/null; then echo "mysql"
    else echo ""
    fi
}

run_query_silent() {
    local cli; cli=$(mysql_client)
    if [ -z "$cli" ]; then
        error "No mariadb/mysql client found."
        return 1
    fi
    if [ -n "$DB_PASS" ]; then
        "$cli" -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" "-p${DB_PASS}" -sN -e "$1" "$DB_NAME" 2>/dev/null
    else
        "$cli" -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -sN -e "$1" "$DB_NAME" 2>/dev/null
    fi
}

# Escape single quotes for SQL string literals
sql_escape() { echo "${1//\'/\'\'}"; }

# ---------------------------------------------------------------------------
# Password hashing: bcrypt(md5_hex(password))
# Mirrors src/AuthServer/Network/PacketHandler.cs:38-79
# ---------------------------------------------------------------------------
hash_password() {
    local pw="$1"
    local md5_hex bcrypted

    if command -v openssl &>/dev/null; then
        md5_hex=$(printf '%s' "$pw" | openssl md5 -r 2>/dev/null | awk '{print $1}')
    elif command -v md5sum &>/dev/null; then
        md5_hex=$(printf '%s' "$pw" | md5sum | awk '{print $1}')
    else
        error "Need openssl or md5sum for password hashing."
        return 1
    fi

    if ! command -v htpasswd &>/dev/null; then
        error "htpasswd missing. Install apache2-utils:  sudo apt install -y apache2-utils"
        return 1
    fi

    # htpasswd emits "user:hash" → strip user and trailing newline; normalize $2y$ → $2a$
    bcrypted=$(htpasswd -bnBC 10 "" "$md5_hex" 2>/dev/null | tr -d '\n' | sed -E 's/^:?//;s/^\$2y\$/\$2a\$/')
    if [ -z "$bcrypted" ]; then
        error "htpasswd returned empty hash."
        return 1
    fi
    echo "$bcrypted"
}

# ---------------------------------------------------------------------------
# Resolve which server entry an arg refers to (returns matching SERVERS[] line)
# ---------------------------------------------------------------------------
resolve_server() {
    local q="${1,,}"
    for entry in "${SERVERS[@]}"; do
        IFS='|' read -r tag dll friendly <<< "$entry"
        if [ "$tag" = "$q" ]; then
            echo "$entry"
            return 0
        fi
    done
    return 1
}

screen_running() {
    # $1 = screen name (e.g. sabine-zone)
    screen -ls 2>/dev/null | grep -qE "\.${1}[[:space:]]"
}

# ---------------------------------------------------------------------------
# Port helpers
# ---------------------------------------------------------------------------
# Returns 0 if $1 is bound (TCP, listening), 1 otherwise.
port_in_use() {
    local port="$1"
    if command -v ss &>/dev/null; then
        ss -ltn "sport = :${port}" 2>/dev/null | tail -n +2 | grep -q .
    elif command -v netstat &>/dev/null; then
        netstat -ltn 2>/dev/null | awk '{print $4}' | grep -qE "[:.]${port}\$"
    else
        # Bash /dev/tcp fallback — connect attempt; busy if it succeeds
        ( exec 3<>/dev/tcp/127.0.0.1/"$port" ) 2>/dev/null && { exec 3<&- 3>&-; return 0; } || return 1
    fi
}

# Describe what's holding the port (best-effort)
port_holder() {
    local port="$1"
    if command -v ss &>/dev/null; then
        ss -ltnp "sport = :${port}" 2>/dev/null | tail -n +2 | head -1
    elif command -v lsof &>/dev/null; then
        lsof -nP -iTCP:"$port" -sTCP:LISTEN 2>/dev/null | tail -n +2 | head -1
    elif command -v netstat &>/dev/null; then
        netstat -ltnp 2>/dev/null | grep -E "[:.]${port}[[:space:]]" | head -1
    fi
}

# First free port at or after $1 (skipping ports in $2 = space-separated reserved list)
find_free_port() {
    local start="$1" reserved="${2:-}"
    local p="$start"
    while [ "$p" -lt 65535 ]; do
        if [[ " $reserved " != *" $p "* ]] && ! port_in_use "$p"; then
            echo "$p"
            return 0
        fi
        p=$((p+1))
    done
    return 1
}

# Server tag → (config_key, conf_file, multi)
# multi=1 means CSV ports, multi=0 means single port.
server_port_meta() {
    case "$1" in
        auth) echo "auth_bind_ports|auth.conf|1" ;;
        char) echo "char_bind_port|char.conf|0" ;;
        zone) echo "zone_bind_port|zone.conf|0" ;;
        web)  echo "web_bind_ports|web.conf|1" ;;
        *)    return 1 ;;
    esac
}

# Read the effective ports for a server (system default with user override applied)
read_server_ports() {
    local tag="$1"
    local meta key file multi
    meta=$(server_port_meta "$tag") || return 1
    IFS='|' read -r key file multi <<< "$meta"
    local val
    val=$(parse_conf "$key" "user/conf/${file}" 2>/dev/null)
    [ -z "$val" ] && val=$(parse_conf "$key" "system/conf/${file}")
    # Split CSV, strip whitespace per line
    echo "$val" | tr ',' '\n' | sed 's/[[:space:]]//g' | grep -v '^$'
}

# Pre-flight: check ports for one server. Prompts user on conflict.
# Returns 0 (start it), 1 (skip it). Writes new port to user conf if chosen.
preflight_ports() {
    local tag="$1" friendly="$2"
    local meta key file multi
    meta=$(server_port_meta "$tag") || return 0
    IFS='|' read -r key file multi <<< "$meta"

    local ports
    ports=$(read_server_ports "$tag")
    if [ -z "$ports" ]; then
        return 0   # nothing to check
    fi

    local conflicts=()
    while IFS= read -r p; do
        [ -z "$p" ] && continue
        if port_in_use "$p"; then
            # If it's our own already-running screen, skip the warning silently
            if screen_running "${SCREEN_PREFIX}-${tag}"; then
                continue
            fi
            conflicts+=("$p")
        fi
    done <<< "$ports"

    if [ "${#conflicts[@]}" -eq 0 ]; then
        return 0
    fi

    echo ""
    warn "${friendly}: port(s) already in use: ${conflicts[*]}"
    for p in "${conflicts[@]}"; do
        local who; who=$(port_holder "$p")
        [ -n "$who" ] && echo -e "    ${YELLOW}port ${p}:${NC} $who"
    done

    local suggested first_conflict
    first_conflict="${conflicts[0]}"
    suggested=$(find_free_port "$((first_conflict + 1))" "$(echo "$ports" | tr '\n' ' ')")

    echo ""
    echo "  Options:"
    echo "    1) Use a different port${suggested:+ (suggested: ${suggested})}"
    echo "    2) Skip ${friendly}"
    echo "    3) Abort"
    read -rp "  Choice [1]: " choice
    choice="${choice:-1}"

    case "$choice" in
        1)
            local new_ports
            if [ "$multi" = "1" ]; then
                # Replace each conflicting port in the CSV; keep non-conflicting ones
                local merged="$ports" rep
                for p in "${conflicts[@]}"; do
                    read -rp "  New port to replace ${p} [${suggested}]: " rep
                    rep="${rep:-$suggested}"
                    if ! [[ "$rep" =~ ^[0-9]+$ ]] || [ "$rep" -lt 1 ] || [ "$rep" -gt 65535 ]; then
                        error "Invalid port."
                        return 1
                    fi
                    if port_in_use "$rep"; then
                        warn "Port ${rep} is also in use — choose another."
                        return 1
                    fi
                    merged=$(echo "$merged" | sed "s/^${p}\$/${rep}/")
                    suggested=$(find_free_port "$((rep + 1))")
                done
                new_ports=$(echo "$merged" | paste -sd, -)
                write_user_conf_kv "user/conf/${file}" "$key" "$new_ports"
                success "${friendly}: ${key} = ${new_ports}"
            else
                read -rp "  New port [${suggested}]: " new_ports
                new_ports="${new_ports:-$suggested}"
                if ! [[ "$new_ports" =~ ^[0-9]+$ ]] || [ "$new_ports" -lt 1 ] || [ "$new_ports" -gt 65535 ]; then
                    error "Invalid port."
                    return 1
                fi
                if port_in_use "$new_ports"; then
                    warn "Port ${new_ports} is also in use."
                    return 1
                fi
                write_user_conf_kv "user/conf/${file}" "$key" "$new_ports"
                success "${friendly}: ${key} = ${new_ports}"
            fi
            return 0
            ;;
        2)
            warn "Skipping ${friendly}"
            return 1
            ;;
        *)
            error "Aborted."
            exit 1
            ;;
    esac
}

# ---------------------------------------------------------------------------
# start / stop / restart / status / logs
# ---------------------------------------------------------------------------
launch_server() {
    local tag="$1" dll="$2" friendly="$3"
    local screen_name="${SCREEN_PREFIX}-${tag}"
    local dll_path="${BINPATH}/${dll}"

    if [ ! -f "$dll_path" ]; then
        echo -e "  ${YELLOW}[SKIP]${NC}  ${friendly} - ${dll} not built (looked in ${BINPATH})"
        return 0
    fi

    if screen_running "$screen_name"; then
        echo -e "  ${YELLOW}[SKIP]${NC}  ${friendly} - already running"
        return 0
    fi

    if ! preflight_ports "$tag" "$friendly"; then
        return 0
    fi

    mkdir -p "$LOGS_DIR"
    local timestamp; timestamp=$(date +'%Y%m%d-%H%M%S')
    local log_file="${LOGS_DIR}/${tag}-${timestamp}.log"

    screen -dmL -Logfile "$log_file" -S "$screen_name" dotnet "$dll_path"
    echo -e "  ${GREEN}[OK]${NC}    ${friendly}  (log: ${log_file})"
}

stop_server() {
    local tag="$1" dll="$2" friendly="$3"
    local screen_name="${SCREEN_PREFIX}-${tag}"

    if screen_running "$screen_name"; then
        screen -S "$screen_name" -X quit >/dev/null 2>&1
        echo -e "  ${GREEN}[OK]${NC}    ${friendly} stopped"
    else
        # Fallback: orphaned dotnet process for this dll?
        local pids
        pids=$(pgrep -f "dotnet .*/${dll}" 2>/dev/null || true)
        if [ -n "$pids" ]; then
            kill $pids 2>/dev/null || true
            echo -e "  ${GREEN}[OK]${NC}    ${friendly} (orphaned process killed)"
        else
            echo -e "  ${YELLOW}[SKIP]${NC}  ${friendly} - not running"
        fi
    fi
}

iter_servers() {
    # $1 = "fwd" or "rev", $2 = filter tag (or empty for all), $3 = function name
    local order="$1" filter="$2" fn="$3"
    local n=${#SERVERS[@]}
    local indices=()
    if [ "$order" = "rev" ]; then
        for ((i=n-1; i>=0; i--)); do indices+=("$i"); done
    else
        for ((i=0; i<n; i++)); do indices+=("$i"); done
    fi
    for i in "${indices[@]}"; do
        IFS='|' read -r tag dll friendly <<< "${SERVERS[$i]}"
        if [ -n "$filter" ] && [ "$filter" != "$tag" ]; then continue; fi
        "$fn" "$tag" "$dll" "$friendly"
    done
}

do_start() {
    require_binpath
    echo -e "${CYAN}Starting Sabine servers...${NC}"
    echo ""
    screen -wipe >/dev/null 2>&1 || true
    iter_servers fwd "$1" launch_server
    # Small delay so Auth is up before Char/Zone reach for it
    sleep 1
    echo ""
    echo -e "${GREEN}Done.${NC} Use './sabine.sh status' to verify."
}

do_stop() {
    echo -e "${CYAN}Stopping Sabine servers...${NC}"
    echo ""
    iter_servers rev "$1" stop_server
    echo ""
    echo -e "${GREEN}Done.${NC}"
}

do_restart() {
    do_stop "$1"
    echo ""
    sleep 2
    do_start "$1"
}

do_status() {
    detect_binpath
    echo -e "${CYAN}Sabine Server Status${NC}"
    if [ -n "$BINPATH" ]; then
        echo -e "  ${BOLD}Binaries:${NC} ${BINPATH}"
    else
        echo -e "  ${YELLOW}Binaries: not built${NC}"
    fi
    echo ""

    local running=0 total=0
    local rows=()
    for entry in "${SERVERS[@]}"; do
        IFS='|' read -r tag dll friendly <<< "$entry"
        # Skip servers that aren't built
        if [ -z "$BINPATH" ] || [ ! -f "${BINPATH}/${dll}" ]; then
            [ "$tag" != "web" ] && rows+=("dim|${friendly}|not built|-|-|-")
            continue
        fi
        total=$((total+1))
        local screen_name="${SCREEN_PREFIX}-${tag}"
        if screen_running "$screen_name"; then
            running=$((running+1))
            local screen_pid dotnet_pid cpu mem rss
            screen_pid=$(screen -ls 2>/dev/null | grep -E "\.${screen_name}[[:space:]]" | awk '{print $1}' | cut -d. -f1 | head -1)
            dotnet_pid=""
            if [ -n "$screen_pid" ]; then
                dotnet_pid=$(ps -o pid= --ppid "$screen_pid" 2>/dev/null | head -1 | tr -d ' ')
            fi
            if [ -z "$dotnet_pid" ]; then
                dotnet_pid=$(pgrep -f "dotnet .*/${dll}" 2>/dev/null | head -1)
            fi
            if [ -n "$dotnet_pid" ]; then
                read -r cpu mem rss <<< "$(ps -o pcpu=,pmem=,rss= -p "$dotnet_pid" 2>/dev/null | head -1)"
                rows+=("up|${friendly}|running|${cpu:-0.0}|${mem:-0.0}|${rss:-0}")
            else
                rows+=("up|${friendly}|running|-|-|-")
            fi
        else
            rows+=("down|${friendly}|stopped|-|-|-")
        fi
    done

    printf "  ${BOLD}%-10s %-10s %6s %6s %12s${NC}\n" "Server" "State" "CPU%" "MEM%" "RSS KB"
    echo "  ───────────────────────────────────────────────"
    for row in "${rows[@]}"; do
        IFS='|' read -r kind name state cpu mem rss <<< "$row"
        local dot
        case "$kind" in
            up)   dot="${GREEN}●${NC}";;
            down) dot="${RED}○${NC}";;
            *)    dot="${YELLOW}·${NC}";;
        esac
        printf "  ${dot} %-8s %-10s %6s %6s %12s\n" "$name" "$state" "$cpu" "$mem" "$rss"
    done
    echo ""
    echo -e "  ${BOLD}${running}/${total}${NC} servers running"
}

do_ports() {
    echo -e "${CYAN}Port Status${NC}"
    echo ""
    printf "  ${BOLD}%-8s %-8s %-7s %s${NC}\n" "Server" "Port" "State" "Holder (if busy)"
    echo "  ────────────────────────────────────────────────────"
    for entry in "${SERVERS[@]}"; do
        IFS='|' read -r tag dll friendly <<< "$entry"
        local ports; ports=$(read_server_ports "$tag")
        if [ -z "$ports" ]; then
            printf "  %-8s %-8s %-7s %s\n" "$friendly" "-" "-" "(no port configured)"
            continue
        fi
        while IFS= read -r p; do
            [ -z "$p" ] && continue
            if port_in_use "$p"; then
                local who; who=$(port_holder "$p" | sed 's/[[:space:]]\+/ /g')
                if screen_running "${SCREEN_PREFIX}-${tag}"; then
                    printf "  ${GREEN}%-8s %-8s %-7s${NC} %s\n" "$friendly" "$p" "ours" "$who"
                else
                    printf "  ${RED}%-8s %-8s %-7s${NC} %s\n" "$friendly" "$p" "BUSY" "$who"
                fi
            else
                printf "  %-8s %-8s ${GREEN}%-7s${NC}\n" "$friendly" "$p" "free"
            fi
        done <<< "$ports"
    done
    echo ""
}

do_logs() {
    local tag="${1:-zone}"
    local entry; entry=$(resolve_server "$tag") || { error "Unknown server '$tag'. Use auth, char, zone, or web."; exit 1; }
    IFS='|' read -r tag dll friendly <<< "$entry"

    local log_file
    log_file=$(ls -t "${LOGS_DIR}/${tag}-"*.log 2>/dev/null | head -1)
    if [ -z "$log_file" ]; then
        warn "No ${friendly} log found in ${LOGS_DIR}/."
        exit 1
    fi
    echo -e "${CYAN}Tailing ${log_file}${NC} (Ctrl+C to stop)"
    echo ""
    tail -f "$log_file"
}

# ---------------------------------------------------------------------------
# update — git pull + rebuild + restart if running
# ---------------------------------------------------------------------------
do_update() {
    echo -e "${CYAN}Updating Sabine...${NC}"
    echo ""

    # Was anything running before update?
    local any_running=false
    for entry in "${SERVERS[@]}"; do
        IFS='|' read -r tag dll friendly <<< "$entry"
        if screen_running "${SCREEN_PREFIX}-${tag}"; then any_running=true; break; fi
    done

    echo -e "  ${BOLD}[1/3]${NC} git pull..."
    if ! git pull --ff-only; then
        error "git pull failed"; exit 1
    fi

    echo -e "  ${BOLD}[2/3]${NC} git submodule update..."
    git submodule update --init --recursive >/dev/null 2>&1 || true

    echo -e "  ${BOLD}[3/3]${NC} dotnet build (Release)..."
    if ! dotnet build Sabine.sln -c Release -v quiet; then
        error "Build failed"; exit 1
    fi

    success "Build complete."

    # Warn about pending SQL updates (don't auto-apply)
    if [ -d "sql/updates" ]; then
        local pending; pending=$(ls -1 sql/updates/*.sql 2>/dev/null | wc -l)
        if [ "$pending" -gt 0 ]; then
            echo ""
            warn "${pending} file(s) in sql/updates/ — review and apply manually if needed."
        fi
    fi

    if [ "$any_running" = true ]; then
        echo ""
        do_stop ""
        sleep 2
        do_start ""
    else
        info "No servers were running — skipping restart."
    fi
}

# ---------------------------------------------------------------------------
# ip — interactive server-IP configuration
# ---------------------------------------------------------------------------
do_ip() {
    echo -e "${CYAN}Server IP Configuration${NC}"
    echo ""
    local current
    current=$(parse_conf "char_server_ip" "user/conf/char.conf" 2>/dev/null)
    [ -z "$current" ] && current=$(parse_conf "char_server_ip" "system/conf/char.conf")
    echo -e "  ${BOLD}Current:${NC} ${current:-127.0.0.1}"
    echo ""

    local wan_ip=""
    if command -v curl &>/dev/null; then
        wan_ip=$(curl -s --max-time 5 ifconfig.me 2>/dev/null || true)
    fi

    echo "    1) Local only (127.0.0.1)"
    if [ -n "$wan_ip" ]; then
        echo "    2) Public IP (${wan_ip})"
        echo "    3) Custom IP"
    else
        echo "    2) Custom IP"
    fi
    echo ""
    read -rp "  Choice [1]: " choice
    choice="${choice:-1}"

    local server_ip="127.0.0.1"
    case "$choice" in
        2)
            if [ -n "$wan_ip" ]; then server_ip="$wan_ip"
            else
                read -rp "  Enter server IP: " server_ip
            fi
            ;;
        3)
            read -rp "  Enter server IP: " server_ip
            ;;
    esac

    while [ -z "$server_ip" ]; do
        read -rp "  IP cannot be empty. Enter server IP: " server_ip
    done

    write_user_conf_kv "user/conf/char.conf" "char_server_ip" "$server_ip"
    write_user_conf_kv "user/conf/zone.conf" "zone_server_ip" "$server_ip"
    success "Wrote char_server_ip and zone_server_ip = ${server_ip}"
    echo ""
    warn "Restart servers for changes to take effect."
}

# ---------------------------------------------------------------------------
# version — pick packet/client version (writes to user/conf/version.conf)
# ---------------------------------------------------------------------------
# Known versions, sourced from system/conf/version.conf comment table.
# Format: code|label|exe-date|note
VERSIONS=(
    "100|iRO Alpha|2001-08-30|supported"
    "200|iRO Beta1|2002-02-20|experimental"
    "300|jRO Beta2|2002-08-09|experimental"
    "350|iRO EP4|2003-04-30|experimental"
    "400|jRO EP3|2003-05-27|experimental"
    "500|iRO EP6|2003-10-31|experimental"
    "600|euRO EP5|2004-05-12|experimental"
    "700|iRO EP8|2004-08-03|experimental"
    "800|bRO EP8|2004-12-28|experimental"
    "2000|euRO EP10|2007-03-05|experimental"
)

write_user_conf_kv() {
    # $1 = file, $2 = key, $3 = value
    local file="$1" key="$2" val="$3"
    mkdir -p "$(dirname "$file")"
    if [ -f "$file" ] && grep -qE "^[[:space:]]*${key}[[:space:]]*:" "$file"; then
        sed -i -E "s|^[[:space:]]*${key}[[:space:]]*:.*$|${key}: ${val}|" "$file"
    else
        if [ ! -f "$file" ]; then
            cat > "$file" <<EOF
// Sabine - User Override
//---------------------------------------------------------------------------

EOF
        fi
        echo "${key}: ${val}" >> "$file"
    fi
}

do_version() {
    echo -e "${CYAN}Client / Packet Version${NC}"
    echo ""

    local current
    current=$(parse_conf "packet_version" "user/conf/version.conf" 2>/dev/null)
    [ -z "$current" ] && current=$(parse_conf "packet_version" "system/conf/version.conf")
    echo -e "  ${BOLD}Current:${NC} ${current:-100}"
    echo ""

    printf "  ${BOLD}%-4s %-3s %-14s %-12s %s${NC}\n" "#" "Ver" "Name" "Exe Date" "Status"
    echo "  ────────────────────────────────────────────────────"
    local i=1
    for entry in "${VERSIONS[@]}"; do
        IFS='|' read -r code label date note <<< "$entry"
        local marker=" "
        [ "$code" = "$current" ] && marker="*"
        local color="$NC"
        [ "$note" = "supported" ] && color="$GREEN"
        printf "  %-2s${marker} %-3s %-14s %-12s ${color}%s${NC}\n" "$i)" "$code" "$label" "$date" "$note"
        i=$((i+1))
    done
    echo ""
    echo "  (* = current)  Only 'supported' versions are officially tested."
    echo ""

    read -rp "  Choice [1-${#VERSIONS[@]}] (Enter to keep current): " choice
    if [ -z "$choice" ]; then
        info "No change."
        return
    fi
    if ! [[ "$choice" =~ ^[0-9]+$ ]] || [ "$choice" -lt 1 ] || [ "$choice" -gt "${#VERSIONS[@]}" ]; then
        error "Invalid choice."
        exit 1
    fi
    IFS='|' read -r code label date note <<< "${VERSIONS[$((choice-1))]}"

    if [ "$note" != "supported" ]; then
        echo ""
        warn "${label} (${code}) is marked experimental. Continue?"
        read -rp "  Type 'yes' to confirm: " confirm
        if [ "$confirm" != "yes" ]; then
            info "Aborted."
            return
        fi
    fi

    write_user_conf_kv "user/conf/version.conf" "packet_version" "$code"
    success "Set packet_version = ${code} (${label})"
    echo ""
    warn "Restart servers for changes to take effect."
}

# ---------------------------------------------------------------------------
# account create / list / admin
# ---------------------------------------------------------------------------
do_account_create() {
    read_db_config

    echo -e "${CYAN}Create Sabine Account${NC}"
    echo ""

    local username
    read -rp "  Username: " username
    while [ -z "$username" ]; do
        warn "Username cannot be empty"
        read -rp "  Username: " username
    done

    local safe_user; safe_user=$(sql_escape "$username")
    local existing; existing=$(run_query_silent "SELECT COUNT(*) FROM accounts WHERE username='${safe_user}';")
    if [ "$existing" != "0" ]; then
        error "Account '${username}' already exists."
        exit 1
    fi

    local pw1 pw2
    while true; do
        read -srp "  Password: " pw1; echo ""
        if [ -z "$pw1" ]; then warn "Password cannot be empty"; continue; fi
        read -srp "  Confirm:  " pw2; echo ""
        if [ "$pw1" = "$pw2" ]; then break; fi
        warn "Passwords do not match"
    done

    echo ""
    echo "  Sex:  M = Male, F = Female"
    local sex_in sex_val
    while true; do
        read -rp "  Sex [M]: " sex_in
        sex_in="${sex_in:-M}"
        case "${sex_in^^}" in
            M) sex_val=1; break;;
            F) sex_val=0; break;;
            *) warn "Enter M or F";;
        esac
    done

    echo ""
    echo "  Authority levels:"
    echo "    0   = Player"
    echo "    50  = Game Master"
    echo "    99  = Administrator"
    echo ""
    local authority
    read -rp "  Authority [0]: " authority
    authority="${authority:-0}"
    if ! [[ "$authority" =~ ^[0-9]+$ ]]; then
        error "Authority must be a number."
        exit 1
    fi

    local hash; hash=$(hash_password "$pw1") || exit 1
    local safe_hash; safe_hash=$(sql_escape "$hash")

    run_query_silent "INSERT INTO accounts (username, password, sex, authority, sessionId) VALUES ('${safe_user}', '${safe_hash}', ${sex_val}, ${authority}, 0);" >/dev/null

    local new_id; new_id=$(run_query_silent "SELECT accountId FROM accounts WHERE username='${safe_user}';")
    if [ -z "$new_id" ]; then
        error "Failed to create account."
        exit 1
    fi
    echo ""
    success "Created account '${username}' (id=${new_id}, authority=${authority})"
}

do_account_list() {
    read_db_config

    echo -e "${CYAN}Sabine Accounts${NC}"
    echo ""

    local count; count=$(run_query_silent "SELECT COUNT(*) FROM accounts;")
    if [ -z "$count" ] || [ "$count" = "0" ]; then
        warn "No accounts found."
        return
    fi

    printf "  ${BOLD}%-6s %-22s %-4s %-10s${NC}\n" "ID" "Username" "Sex" "Authority"
    echo "  ──────────────────────────────────────────────"

    run_query_silent "SELECT accountId, username, sex, authority FROM accounts ORDER BY accountId;" \
    | while IFS=$'\t' read -r id name sex auth; do
        local sex_label="?"
        [ "$sex" = "1" ] && sex_label="M"
        [ "$sex" = "0" ] && sex_label="F"
        local auth_label
        if [ "$auth" -ge 99 ] 2>/dev/null; then
            auth_label="${RED}Admin${NC}"
        elif [ "$auth" -ge 50 ] 2>/dev/null; then
            auth_label="${YELLOW}GM${NC}"
        else
            auth_label="${GREEN}Player${NC}"
        fi
        printf "  %-6s %-22s %-4s %-3s (${auth_label})\n" "$id" "$name" "$sex_label" "$auth"
    done

    echo ""
    echo -e "  ${BOLD}Total: ${count}${NC}"
}

do_account_admin() {
    local username="$1"
    if [ -z "$username" ]; then
        error "Usage: $0 account admin <username>"
        exit 1
    fi
    read_db_config

    local safe_user; safe_user=$(sql_escape "$username")
    local existing; existing=$(run_query_silent "SELECT COUNT(*) FROM accounts WHERE username='${safe_user}';")
    if [ -z "$existing" ] || [ "$existing" = "0" ]; then
        error "Account '${username}' not found."
        exit 1
    fi

    run_query_silent "UPDATE accounts SET authority=99 WHERE username='${safe_user}';" >/dev/null
    success "Account '${username}' promoted to Administrator (authority=99)."
}

do_account() {
    case "${1:-}" in
        create) do_account_create ;;
        list)   do_account_list ;;
        admin)  do_account_admin "$2" ;;
        *)
            echo "Usage: $0 account {create|list|admin <username>}"
            exit 1
            ;;
    esac
}

# ---------------------------------------------------------------------------
# install — one-shot setup for Debian/Ubuntu
# ---------------------------------------------------------------------------
generate_password() { tr -dc 'A-Za-z0-9' < /dev/urandom | head -c 24; }

do_install() {
    echo -e "${CYAN}"
    echo "  ╔══════════════════════════════════════════════╗"
    echo "  ║          Sabine Server Installer             ║"
    echo "  ╚══════════════════════════════════════════════╝"
    echo -e "${NC}"

    if [ "$EUID" -ne 0 ]; then
        error "Run as root: sudo ./sabine.sh install"
        exit 1
    fi

    if [ ! -f /etc/os-release ]; then
        error "Cannot detect OS. This installer supports Debian/Ubuntu only."
        exit 1
    fi
    . /etc/os-release
    if [[ "$ID" != "debian" && "$ID" != "ubuntu" ]]; then
        error "Unsupported OS: $ID. Debian/Ubuntu only."
        exit 1
    fi
    success "Detected ${ID^} ${VERSION_ID}"

    local real_user="${SUDO_USER:-}"
    local real_group=""
    [ -n "$real_user" ] && real_group=$(id -gn "$real_user" 2>/dev/null || echo "$real_user")

    if [ ! -f Sabine.sln ]; then
        error "Sabine.sln not found in $(pwd). Run install from the repo root."
        exit 1
    fi

    # ---- Step 1: System packages ----
    step "1/8" "System Dependencies"
    info "Updating apt..."
    apt-get update -qq
    info "Installing core packages..."
    DEBIAN_FRONTEND=noninteractive apt-get install -y -qq \
        screen git curl wget unzip apt-transport-https ca-certificates \
        gnupg lsb-release libcap2-bin apache2-utils >/dev/null 2>&1
    success "Core packages installed (screen, git, curl, apache2-utils, ...)"

    if command -v mariadb &>/dev/null; then
        success "MariaDB already installed"
    else
        info "Installing MariaDB..."
        DEBIAN_FRONTEND=noninteractive apt-get install -y -qq mariadb-server >/dev/null 2>&1
        success "MariaDB installed"
    fi
    systemctl start mariadb
    systemctl enable mariadb >/dev/null 2>&1
    success "MariaDB service running"

    if command -v dotnet &>/dev/null && dotnet --list-sdks | grep -q "^8\."; then
        success ".NET SDK 8 already installed: $(dotnet --version)"
    else
        info "Installing .NET SDK 8.0..."
        local dotnet_os="$ID" dotnet_ver="$VERSION_ID"
        if [ "$ID" = "debian" ]; then
            case "$VERSION_ID" in
                13|14) dotnet_ver="12"; warn "Debian ${VERSION_ID} not supported by Microsoft, using bookworm/12 repo";;
            esac
        fi
        wget -q "https://packages.microsoft.com/config/${dotnet_os}/${dotnet_ver}/packages-microsoft-prod.deb" -O /tmp/packages-microsoft-prod.deb
        dpkg -i /tmp/packages-microsoft-prod.deb >/dev/null 2>&1
        rm -f /tmp/packages-microsoft-prod.deb
        apt-get update -qq
        DEBIAN_FRONTEND=noninteractive apt-get install -y -qq dotnet-sdk-8.0 >/dev/null 2>&1
        if ! command -v dotnet &>/dev/null; then
            error ".NET SDK installation failed. Install manually: https://dotnet.microsoft.com/download/dotnet/8.0"
            exit 1
        fi
        success ".NET SDK 8 installed: $(dotnet --version)"
    fi

    # ---- Step 2: Database ----
    step "2/8" "Database Setup"
    DB_NAME="sabine"
    DB_USER="sabine"
    DB_PASS=$(generate_password)
    DB_HOST="127.0.0.1"
    DB_PORT="3306"

    info "Creating database '${DB_NAME}' and user '${DB_USER}'..."
    mariadb -u root <<EOF
CREATE DATABASE IF NOT EXISTS \`${DB_NAME}\` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
CREATE USER IF NOT EXISTS '${DB_USER}'@'localhost' IDENTIFIED BY '${DB_PASS}';
ALTER USER '${DB_USER}'@'localhost' IDENTIFIED BY '${DB_PASS}';
GRANT ALL PRIVILEGES ON \`${DB_NAME}\`.* TO '${DB_USER}'@'localhost';
FLUSH PRIVILEGES;
EOF
    success "Database and user ready"

    # ---- Step 3: Schema ----
    step "3/8" "Import Schema"
    if mariadb -u "$DB_USER" "-p${DB_PASS}" "$DB_NAME" -e "SELECT 1 FROM accounts LIMIT 1" >/dev/null 2>&1; then
        success "Schema already imported (accounts table exists)"
    else
        info "Importing sql/main.sql..."
        mariadb -u "$DB_USER" "-p${DB_PASS}" "$DB_NAME" < sql/main.sql
        success "Schema imported"
    fi

    # ---- Step 4: Build ----
    step "4/8" "Build Sabine"
    info "dotnet restore..."
    dotnet restore Sabine.sln --verbosity quiet
    info "dotnet build (Release)..."
    dotnet build Sabine.sln --configuration Release --verbosity quiet
    success "Build complete"

    # ---- Step 5: User configs ----
    step "5/8" "Database Configuration"
    mkdir -p user/conf

    if [ -f user/conf/database.conf ]; then
        warn "user/conf/database.conf exists — backing up to .bak"
        cp user/conf/database.conf user/conf/database.conf.bak
    fi
    cat > user/conf/database.conf <<EOF
// Sabine - Database Configuration (generated by sabine.sh on $(date '+%Y-%m-%d %H:%M:%S'))
//---------------------------------------------------------------------------

db_host: ${DB_HOST}
db_port: ${DB_PORT}
db_user: ${DB_USER}
db_pass: ${DB_PASS}
db_name: ${DB_NAME}
EOF
    success "Wrote user/conf/database.conf"

    # ---- Step 6a: Packet version ----
    step "6/8" "Client / Packet Version"
    echo "  Sabine supports multiple Ragnarok client versions."
    echo "  Default is iRO Alpha (100, the only officially supported one)."
    echo ""
    echo "  Pick a version now? Otherwise iRO Alpha (100) will be used."
    read -rp "  Configure version? [y/N]: " ver_yn
    case "${ver_yn,,}" in
        y|yes) do_version ;;
        *)     info "Using default: iRO Alpha (100)" ;;
    esac

    # ---- Step 7: IP ----
    step "7/8" "Server IP"
    local wan_ip=""
    command -v curl &>/dev/null && wan_ip=$(curl -s --max-time 5 ifconfig.me 2>/dev/null || true)

    echo "    1) Local only (127.0.0.1)"
    if [ -n "$wan_ip" ]; then
        echo "    2) Public IP (${wan_ip})"
        echo "    3) Custom IP"
    else
        echo "    2) Custom IP"
    fi
    echo ""
    read -rp "  Choice [1]: " ip_choice
    ip_choice="${ip_choice:-1}"
    local server_ip="127.0.0.1"
    case "$ip_choice" in
        2) [ -n "$wan_ip" ] && server_ip="$wan_ip" || read -rp "  Enter server IP: " server_ip ;;
        3) read -rp "  Enter server IP: " server_ip ;;
    esac
    [ -z "$server_ip" ] && server_ip="127.0.0.1"

    if [ "$server_ip" != "127.0.0.1" ]; then
        write_user_conf_kv "user/conf/char.conf" "char_server_ip" "$server_ip"
        write_user_conf_kv "user/conf/zone.conf" "zone_server_ip" "$server_ip"
        success "Server IP set to ${server_ip}"
    else
        info "Using default localhost"
    fi

    # ---- Step 8: Admin account ----
    step "8/8" "Admin Account"
    local admin_user admin_pass admin_pass2 admin_sex admin_sex_val
    read -rp "  Admin username: " admin_user
    while [ -z "$admin_user" ]; do read -rp "  Admin username: " admin_user; done
    while true; do
        read -srp "  Admin password: " admin_pass; echo ""
        [ -z "$admin_pass" ] && { warn "Password cannot be empty"; continue; }
        read -srp "  Confirm:        " admin_pass2; echo ""
        [ "$admin_pass" = "$admin_pass2" ] && break
        warn "Passwords do not match"
    done
    read -rp "  Sex (M/F) [M]: " admin_sex
    case "${admin_sex:-M}" in F|f) admin_sex_val=0 ;; *) admin_sex_val=1 ;; esac

    local admin_hash; admin_hash=$(hash_password "$admin_pass")
    local safe_admin; safe_admin=$(sql_escape "$admin_user")
    local safe_hash; safe_hash=$(sql_escape "$admin_hash")
    mariadb -u "$DB_USER" "-p${DB_PASS}" "$DB_NAME" <<EOF
INSERT INTO accounts (username, password, sex, authority, sessionId)
VALUES ('${safe_admin}', '${safe_hash}', ${admin_sex_val}, 99, 0)
ON DUPLICATE KEY UPDATE password='${safe_hash}', authority=99;
EOF
    success "Admin account '${admin_user}' created (authority=99)"

    # Make this script executable + fix ownership
    chmod +x sabine.sh 2>/dev/null || true
    if [ -n "$real_user" ] && [ "$real_user" != "root" ]; then
        chown -R "${real_user}:${real_group}" .
        success "Ownership reset to ${real_user}"
    fi

    # ---- Summary ----
    echo ""
    echo -e "${CYAN}══════════════════════════════════════════════════${NC}"
    echo -e "${GREEN}${BOLD}  Installation complete${NC}"
    echo -e "${CYAN}══════════════════════════════════════════════════${NC}"
    echo ""
    echo -e "  ${BOLD}Database:${NC}      ${DB_NAME}"
    echo -e "  ${BOLD}DB user:${NC}       ${DB_USER}"
    echo -e "  ${BOLD}DB password:${NC}   ${DB_PASS}"
    echo -e "  ${BOLD}Server IP:${NC}     ${server_ip}"
    echo -e "  ${BOLD}Admin:${NC}         ${admin_user}"
    echo ""
    echo -e "  ${YELLOW}⚠  Save these — they won't be shown again.${NC}"
    echo ""
    echo -e "  ${BOLD}Next steps:${NC}"
    echo "    ./sabine.sh start       # start auth/char/zone/web"
    echo "    ./sabine.sh status"
    echo "    ./sabine.sh logs zone"
    echo ""
}

# ---------------------------------------------------------------------------
# help
# ---------------------------------------------------------------------------
do_help() {
    printf "%b\n" "${BOLD}Sabine Server & Account Manager${NC}"
    echo ""
    echo "Usage: $0 <command> [args]"
    echo ""
    printf "%b\n" "  ${BOLD}install${NC}                       One-time setup (Debian/Ubuntu, requires sudo)"
    printf "%b\n" "  ${BOLD}start${NC} [auth|char|zone|web]    Start servers (default: all)"
    printf "%b\n" "  ${BOLD}stop${NC}  [auth|char|zone|web]    Stop servers"
    printf "%b\n" "  ${BOLD}restart${NC}                       Stop then start"
    printf "%b\n" "  ${BOLD}status${NC}                        Show running servers and resource usage"
    printf "%b\n" "  ${BOLD}logs${NC}  [auth|char|zone|web]    Tail latest log (default: zone)"
    printf "%b\n" "  ${BOLD}update${NC}                        git pull + rebuild + restart-if-running"
    printf "%b\n" "  ${BOLD}ip${NC}                            Configure server IP for client connections"
    printf "%b\n" "  ${BOLD}version${NC}                       Pick client/packet version (alpha, beta1, ...)"
    printf "%b\n" "  ${BOLD}ports${NC}                         Show configured ports and which are free / busy"
    echo ""
    printf "%b\n" "  ${BOLD}account create${NC}                Create a new game account"
    printf "%b\n" "  ${BOLD}account list${NC}                  List all accounts"
    printf "%b\n" "  ${BOLD}account admin${NC} <username>      Promote account to Admin (authority 99)"
    echo ""
    echo "Each server runs in a detached screen session named '${SCREEN_PREFIX}-<name>'."
    echo "Attach with:  screen -r ${SCREEN_PREFIX}-zone"
}

# ---------------------------------------------------------------------------
# Dispatch
# ---------------------------------------------------------------------------
case "${1:-}" in
    install) shift; do_install "$@" ;;
    start)   shift; tag="${1:-}"; if [ -n "$tag" ]; then resolve_server "$tag" >/dev/null || { error "Unknown server '$tag'"; exit 1; }; fi; do_start "$tag" ;;
    stop)    shift; tag="${1:-}"; if [ -n "$tag" ]; then resolve_server "$tag" >/dev/null || { error "Unknown server '$tag'"; exit 1; }; fi; do_stop "$tag" ;;
    restart) shift; tag="${1:-}"; if [ -n "$tag" ]; then resolve_server "$tag" >/dev/null || { error "Unknown server '$tag'"; exit 1; }; fi; do_restart "$tag" ;;
    status)  do_status ;;
    logs)    shift; do_logs "$1" ;;
    update)  do_update ;;
    ip)      do_ip ;;
    version) do_version ;;
    ports)   do_ports ;;
    account) shift; do_account "$@" ;;
    help|-h|--help|"") do_help ;;
    *) error "Unknown command: $1"; echo ""; do_help; exit 1 ;;
esac

exit 0
