# Microled NFE LocalAgent on Windows

## Recommended execution mode

For A3 certificates that require PIN entry, run the LocalAgent as a normal desktop process in the logged-in user's session.

Do not run it as:

- a Windows Service
- a scheduled task running without interactive desktop access
- another Windows user account

This is the most reliable mode because the token middleware can open the PIN dialog when the private key is used.

## Recommended flow

1. Log in to Windows with the same user that has access to the certificate/token.
2. Ensure the token middleware/driver is installed and the token is connected.
3. Publish the LocalAgent for Windows:

```bat
publish-win-x64.cmd
```

4. Go to the publish folder:

```text
bin\Release\net8.0\publish\win-x64\
```

5. Adjust `appsettings.json` in that publish folder if needed.
6. Start the LocalAgent in the same user session:

```bat
run-localagent.cmd
```

7. Keep the console window open while using the frontend.

## Why this mode is preferred

- Keeps the LocalAgent in the interactive desktop session
- Allows A3 middleware to prompt for PIN
- Avoids service-account access issues with `CurrentUser\My`
- Avoids requiring the .NET runtime on the client machine because the publish profile is self-contained

## Quick health check

After starting the LocalAgent, test:

```bat
curl http://localhost:5278/api/local/health
```

## Notes

- The selected certificate profile is stored under `%ProgramData%\Microled\Nfe\localagent\profiles.json`
- If the certificate changes, restart the LocalAgent if the token middleware keeps stale state
- If the token requires a PIN dialog, approve it on the Windows desktop when prompted
