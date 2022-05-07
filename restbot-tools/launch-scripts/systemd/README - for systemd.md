# Automating the launch in systemd

This should get RESTbot up and running under Debian/Ubuntu Linux and its derivatives (e.g. RaspberryOS).

For other systems, please adjust the paths below accordingly.

## Requirements

- `screen` (which can be installed with `apt-get install screen`) 

## Instructions

- copy `restbot.service.sample` to `restbot.service`
- fill in the missing data, namely:
	* User (the account that this script will run under; avoid `root` at all costs!)
	* Group
	* adjust the paths for the installation, paying attention that the working directory should be the actual  
  directory where the binary resides
- let systemd be aware of the existence of the file, running (as root) `/usr/bin/systemctl link /path/to/your/restbot/directory/restbot-tools/launch-scripts/systemd/restbot.service`
- enable the service: `/usr/bin/systemctl enable /path/to/your/restbot/directory/restbot-tools/launch-scripts/systemd/restbot.service`
- from now on, you can safely start and stop the service using `systemctl start restbot` and `systemctl stop restbot`

If, by any chance, you need to change this script, don't forget to run `systemctl daemon-reload` after saving, so that systemd is made aware of the changes.