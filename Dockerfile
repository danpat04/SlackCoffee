# base image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 as base
WORKDIR /app
EXPOSE 80

# build image
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build
WORKDIR /SlackCoffee
COPY . .
RUN dotnet publish -c Release -o /app

FROM base as final
WORKDIR /app
COPY --from=build /app .
RUN ls -al
ENTRYPOINT ["./SlackCoffee", "--urls=http://0.0.0.0:80"]
