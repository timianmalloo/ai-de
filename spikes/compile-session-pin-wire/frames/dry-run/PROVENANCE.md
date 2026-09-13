# Redaction

Every captured frame, both directions, has email-shaped substrings replaced with `redacted@example.invalid` before being written to disk — naming the rule, not the values, is how a redaction undoes itself. Found live in `_auth/status_update`'s `authStatus.account.{email,organization}` (the operator's account identity). No other field is known to carry account-identifying data as of this run; a future frame method that does would need its own rule here.
