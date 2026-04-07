FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY AcmeEHRDataProcessingAPI.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish AcmeEHRDataProcessingAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 5071

ENV ASPNETCORE_URLS=http://+:5071
ENV MongoDB__ConnectionString=mongodb://host.docker.internal:27017

ENTRYPOINT ["dotnet", "AcmeEHRDataProcessingAPI.dll"]