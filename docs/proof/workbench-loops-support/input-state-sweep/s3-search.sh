#!/bin/sh
# S3 recall search (B5): tests that assert a focus or foreground precondition as a FAIL.
# Brush properties named Foreground are not input state, so the pattern names the foreground WINDOW forms.
cd C:/Projects/ai-de-wl-input-state-sweep || exit 1
grep -rn -E "GetForegroundWindow|SetForegroundWindow|desk run owed|focus could not|foreground (is |not )?held|foreground window|FOREGROUND|Assert\.[A-Za-z]+\(.*(Keyboard\.FocusedElement|IsKeyboardFocus|IsActive)|Assert\.[A-Za-z]+\(.*[Ff]ocus.*(environment|fixture could not)" tests --include=*.cs \
  | grep -v "/bin/\|/obj/"
