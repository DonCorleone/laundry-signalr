# Docker

## Create

docker build --platform linux/amd64 -t doncorleone/laundrysignalr:init .

## Push

docker push doncorleone/laundrysignalr:init

## Registry

[hub.docker.com](https://hub.docker.com/r/doncorleone/laundrysignalr/tags)

## Redis

brew tap ringohub/redis-cli

brew update && brew doctor

brew install redis-cli

## User Secrets
    
```xml
<PropertyGroup>
    <UserSecretsId>your-guid-here</UserSecretsId>
</PropertyGroup>
```

```bash
dotnet user-secrets init
```

```bash
dotnet user-secrets list
```

```bash
dotnet user-secrets set "MY_ENV_VAR" "YourSecretValue"
```

```csharp
string secretValue = Environment.GetEnvironmentVariable("MY_ENV_VAR");

if (string.IsNullOrEmpty(secretValue))
{
    Console.WriteLine("Environment variable not found.");
}
else
{
    Console.WriteLine($"Environment variable value: {secretValue}");
}
```