## Source Code Instructions
### Regular Soure Code
After getting the project from github, you need to have .NET 8.0 minimum to run this project as a C# project. Running as a C# project should just take you directly to the swagger page on localhost:5071/swagger/index.html.
### Docker From Soure Code (Easier)
After getting the project from github, go to the command line in the root folder (the folder with the AcmeEHRDataProcessingAPI.csproj) and run these commands:
- docker build -t acme-ehr-api .
- docker run -p 5071:5071 acme-ehr-api

This should run the API server on localhost:5071. You can checkout the swagger page to confirm that things are running correctly by going to localhost:5071/swagger/index.html.

## DockerHub (Easiest)
Go to command line and run these commands:
- docker pull nsaigal/acme-ehr-api:latest
- docker run -p 5071:5071 nsaigal/acme-ehr-api

Again, this should run the API server on localhost:5071. You can checkout the swagger page to confirm that things are running correctly by going to localhost:5071/swagger/index.html.
