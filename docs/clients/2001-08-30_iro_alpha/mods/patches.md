iRO Alpha Client Patches
=============================================================================

While the alpha client runs relatively well on a modern machine, there
are a few minor issues that should be addressed before it's actively
used. This file documents the recommended modifications.

These changes can be made either by manually modifying the RagExe by
use of a hex editor, or you can use the memory patcher available at
the following address, which implements these modifications.

https://github.com/exectails/HookCatRO

Clean Text
-----------------------------------------------------------------------------

Many older clients have a problem with rendering text under modern
Windows, giving some strings a pink border. To fix this, all three
calls to CreateFontA need to be patched.

Simply replace all occurrences of this:
```text
50 6A 10 6A 00 6A 00 6A 00 6A 01 6A 00 6A 00 6A 00 68 90010000 6A 00 6A 00 6A 00 51 FF15
```

With this:
```text
50 6A 10 6A 03 6A 00 6A 00 6A 01 6A 00 6A 00 6A 00 68 90010000 6A 00 6A 00 6A 00 51 FF15
```

This modifies the quality parameter of the created font from 0 (DEFAULT_QUALITY)
to 3 (NONANTIALIASED_QUALITY), so the text doesn't get anti-aliased.

What happens to the text with the default quality is different from what
happened back in Windows 98, the system this client was designed for, and
the client basically needs to be told to not do that.

Connection Address
-----------------------------------------------------------------------------

A rather vital modification is changing the address the client connects
to, because you wouldn't be able to connect to a server emulator otherwise.
For this, simply replace the IP string in the Ragexe.exe with the IP you
want to connect to.

For example, to make it connect to localhost (127.0.0.1), replace this:
```text
32 31 31 2E 32 33 39 2E 31 32 33 2E 31 36 38
```
(211.239.123.168)

With this:
```text
31 32 37 2E 30 2E 30 2E 31 00 00 00 00 00 00
```
(127.0.0.1)

Make sure to not add or remove any bytes, and either fill up the rest
of the IP string with 0 bytes, or at least put one at the end of your
custom IP.

The default port the client connects to is 7000, though this can be changed
easily as well, by replacing this:
```text
C705 70605200 581B0000
```

With this:
```text
C705 70605200 XXXXXXXX
```

Where XXXXXXXX is the port you want to connect to in hexadecimal (little
endian).

DDraw Errors
-----------------------------------------------------------------------------

When the client closes, you might get errors about DDraw instances still
having been referenced. This gets pretty annoying over time, but it's
easy to silence the client, and the information is relatively unimportant
anyway.

To remove this message, simply replace this:
```text
8BD8 3BDF 7E 2B 53 8D5424
```

With this:
```text
8BD8 3BDF EB 2B 53 8D5424
```

Multi-Client
-----------------------------------------------------------------------------

Quick and dirty, replace this:
```text
FF15 1C224D00 85C0 74 09
```

With this:
```text
FF15 1C224D00 85C0 EB 09
```

Disclaimer
-----------------------------------------------------------------------------

When the client starts, it displays a disclaimer. It's simple enough
to just accept it, but it gets annoying over time. To disable it,
replace this:
```text
B9 68295200 C705 04615200 0A000000
```

With this:
```text
B9 68295200 C705 04615200 00000000
```

Window Size
-----------------------------------------------------------------------------

By default, the client runs only in full-screen or in 640x480 window mode.
That's a pretty small window on a modern system, so you will probably want
to modify it.

To change the resolution, replace this:
```text
53 68 E0010000 68 80020000 6A 01 FFD6 2D E0010000 99 2BC2 D1F8 50 53 FFD6 2D 80020000
```

With this:
```text
53 68 HHHHHHHH 68 WWWWWWWW 6A 01 FFD6 2D HHHHHHHH 99 2BC2 D1F8 50 53 FFD6 2D WWWWWWWW
```

And this:
```text
8D843A E0010000 8B7C24 28 2BCF 50 8B4424 18 8D9431 80020000
```

With this:
```text
8D843A HHHHHHHH 8B7C24 28 2BCF 50 8B4424 18 8D9431 WWWWWWWW
```

WWWWWWWW is the width you want the client to run in in hexadecimal, and
HHHHHHHH is the height. For example, for 1024x768, you would use these
replacements:
```text
53 68 00030000 68 00040000 6A 01 FFD6 2D 00030000 99 2BC2 D1F8 50 53 FFD6 2D 00040000
8D843A 00030000 8B7C24 28 2BCF 50 8B4424 18 8D9431 00040000
```

Chat Spam
-----------------------------------------------------------------------------

To not be bothered by the client trying to prevent your from sending
a chat message more than twice, replace this:
```text
C785 80020000 00000000 83BD 80020000 02 0F8D 23020000
```

With this:
```text
C785 80020000 00000000 83BD 80020000 02 9090 90909090
```

Alternatively, you can also change the number of times a message can
be repeated, by replacing with this:
```text
C785 80020000 00000000 83BD 80020000 XX 0F8D 23020000
```

Where XX is the number of times you can repeat a message.

Bonus Tip
-----------------------------------------------------------------------------

The last tip for the Alpha is to get [dgVoodoo][1]. The client's
performance is a little wonky by default, and especially the mouse
cursor is laggy to a point where it's bothersome. By using dgVoodoo,
the client's DirectX calls will be upgraded to newer versions and it
will run better by a considerable amount. It's as easy as downloading
it and placing a few DLL files in the client's folder, so there's very
little reason not to do it.


[1]: http://dege.freeweb.hu/dgVoodoo2/
