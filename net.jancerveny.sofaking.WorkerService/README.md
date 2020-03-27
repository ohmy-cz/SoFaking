### SoFaking Worker Service
This service (or daemon in Linux terms), should be running constantly on the host system. Its responsibilities are:
- When a movie downloading finishes, rename the folder to the correct IMD name, delete unnecessary content, and add a cover image
- Download subtitles for pre-configured languages
- Transcode movies to PS4-compatible format with FFMPEG
- Watch for movie releases according to movies watched in the database
- Automatically download highest rated (8+) movies every day until the disk is full to 50%, in a specific folder
- Update the frontend with the actual status