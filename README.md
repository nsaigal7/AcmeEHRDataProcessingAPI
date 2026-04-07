To Build from Source Code:

- docker build -t acme-ehr-api .
- docker run -p 5071:5071 acme-ehr-api

Or get it from DockerHub:
- docker pull nsaigal/acme-ehr-api:latest
- docker run -p 5071:5071 nsaigal/acme-ehr-api

And then go to localhost:5071/swagger/index.html