#!/usr/bin/env python3
"""Port cast timings from rathena's pre-renewal skill DB into system/data/skills.txt.

Sabine's skill data has always supported a `cast:` block (Shared/Data/Databases/Skills.cs),
but no entry ever populated it, so GetEffectiveCastTime() returned 0 for every skill and
ScheduleSkillCast() skipped the ZC_USESKILL_ACK broadcast entirely -- no client ever saw a
cast bar. This backfills that block from rathena.

Arrays are emitted dense and maxLevel-long on purpose: Skill.GetCastTime() indexes
CastTime[level - 1] with no bounds check, so a short array throws at cast time.

Cast times are the pre-DEX base values. Sabine applies pre-renewal DEX scaling and the
Suffragium/Bragi reductions itself in Skill.GetEffectiveCastTime, so they port across as-is.

Usage:
    python tools/port_cast_times.py [--rathena PATH] [--write]

Without --write it reports what it would change and touches nothing.
"""

import argparse
import re
import sys

import yaml

ENTRY_RE = re.compile(r'^(\s*\{ id: (\d+), skillId: "([A-Za-z0-9_]+)".*?)(,?)\s*$')
MAXLEVEL_RE = re.compile(r'maxLevel:\s*(\d+)')

# rathena field -> Sabine field
FIELDS = [
    ("CastTime", "castTime"),
    ("AfterCastActDelay", "afterCastActDelay"),
    ("Cooldown", "cooldown"),
]


def densify(value, max_level):
    """Expand a rathena timing into a dense max_level-long list.

    rathena writes these either as a scalar (applies to every level) or as a sparse
    per-level list, where levels that aren't listed inherit the last listed value.
    """
    if value is None:
        return None
    if isinstance(value, int):
        return [value] * max_level

    by_level = {}
    for row in value:
        lv = row.get("Level")
        if lv is None:
            continue
        by_level[int(lv)] = int(row.get("Time", 0) or 0)
    if not by_level:
        return None

    out, carry = [], by_level[min(by_level)]
    for lv in range(1, max_level + 1):
        if lv in by_level:
            carry = by_level[lv]
        out.append(carry)
    return out


def load_rathena(path):
    with open(path, encoding="utf-8") as f:
        doc = yaml.safe_load(f)
    return {s["Name"]: s for s in doc.get("Body", []) if "Name" in s}


def build_cast_block(skill, max_level):
    parts = []
    for ra_field, sabine_field in FIELDS:
        values = densify(skill.get(ra_field), max_level)
        if not values or not any(values):
            continue
        parts.append("%s: [%s]" % (sabine_field, ", ".join(str(v) for v in values)))
    if not parts:
        return None
    return "cast: { %s }" % ", ".join(parts)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--rathena", default="C:/Projects/GitHub/rathena/db/pre-re/skill_db.yml")
    ap.add_argument("--skills", default="system/data/skills.txt")
    ap.add_argument("--write", action="store_true")
    args = ap.parse_args()

    rathena = load_rathena(args.rathena)

    with open(args.skills, "rb") as f:
        lines = f.read().decode("utf-8").split("\n")

    updated, skipped_missing, skipped_instant, already = 0, [], 0, 0
    for i, line in enumerate(lines):
        m = ENTRY_RE.match(line)
        if not m:
            continue
        body, _skill_id, name, comma = m.groups()

        if "cast:" in body:
            already += 1
            continue

        skill = rathena.get(name)
        if skill is None:
            skipped_missing.append(name)
            continue

        lm = MAXLEVEL_RE.search(body)
        max_level = int(lm.group(1)) if lm else 1

        block = build_cast_block(skill, max_level)
        if block is None:
            skipped_instant += 1
            continue

        # Splice in ahead of the entry's closing brace.
        close = body.rstrip().rfind("}")
        lines[i] = "%s, %s }%s" % (body[:close].rstrip().rstrip(","), block, comma)
        updated += 1

    print("entries updated:      %d" % updated)
    print("already had cast:     %d" % already)
    print("no timings in rathena:%d (instant-cast, left alone)" % skipped_instant)
    print("not found in rathena: %d %s" % (len(skipped_missing), skipped_missing[:8]))

    if args.write:
        with open(args.skills, "wb") as f:
            f.write("\n".join(lines).encode("utf-8"))
        print("wrote %s" % args.skills)
    else:
        print("(dry run; pass --write to apply)")

    return 0


if __name__ == "__main__":
    sys.exit(main())
