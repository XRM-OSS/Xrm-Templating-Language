# Base image: Node 18 + .NET SDK 6
FROM mcr.microsoft.com/devcontainers/dotnet:0-6.0

# Install Node.js 18 (overwrite if not already present)
RUN curl -fsSL https://deb.nodesource.com/setup_18.x | bash - \
  && apt-get update && apt-get install -y nodejs

# Set user to vscode (standard user in devcontainers)
USER vscode
