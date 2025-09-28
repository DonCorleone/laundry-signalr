#!/bin/bash

# LaundrySignalR Development Setup Script
# This script helps new team members set up their local development environment

echo "🚀 Setting up LaundrySignalR development environment..."

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK is not installed. Please install .NET 8.0 SDK first."
    echo "   Download from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ .NET SDK found"

# Check if appsettings files exist
if [ ! -f "appsettings.json" ]; then
    echo "📄 Creating appsettings.json from template..."
    cp appsettings.json.template appsettings.json
    echo "⚠️  Please update the MongoDB connection string in appsettings.json"
fi

if [ ! -f "appsettings.Development.json" ]; then
    echo "📄 Creating appsettings.Development.json from template..."
    cp appsettings.Development.json.template appsettings.Development.json
fi

# Restore packages
echo "📦 Restoring NuGet packages..."
dotnet restore

# Setup user secrets for development
echo "🔐 Setting up user secrets..."
echo "Please provide your MongoDB connection details:"

read -p "MongoDB Connection String (or press Enter for local): " MONGO_CONN
if [ -z "$MONGO_CONN" ]; then
    MONGO_CONN="mongodb://localhost:27017"
fi

read -p "MongoDB Database Name [laundry-calendar-dev]: " MONGO_DB
if [ -z "$MONGO_DB" ]; then
    MONGO_DB="laundry-calendar-dev"
fi

# Set user secrets
dotnet user-secrets set "MongoDB:ConnectionString" "$MONGO_CONN"
dotnet user-secrets set "MongoDB:DatabaseName" "$MONGO_DB"

echo "✅ User secrets configured"

# Build the project
echo "🔨 Building project..."
dotnet build

if [ $? -eq 0 ]; then
    echo "🎉 Setup completed successfully!"
    echo ""
    echo "📋 Next steps:"
    echo "   1. Start your MongoDB instance (if using local)"
    echo "   2. Run: dotnet run"
    echo "   3. The application will be available at http://localhost:5263"
    echo ""
    echo "🔧 Configuration:"
    echo "   - User secrets are stored securely on your machine"
    echo "   - appsettings.json files are excluded from git"
    echo "   - Use 'dotnet user-secrets list' to view your secrets"
else
    echo "❌ Build failed. Please check the errors above."
    exit 1
fi