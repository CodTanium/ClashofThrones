Bug: pause menu opened by itself once in a test run

**Seen:** 2026-11-21, one windowed screenshot run showed the pause menu right after starting a new game. Not reproduced in 5+ later runs.
**Cause:** unknown (suspects: a stray Escape/Start event while the window got focus, or a virtual gamepad).

**Next step:** log the event that opens the pause menu (device, key) in debug builds; close if it never shows up again.