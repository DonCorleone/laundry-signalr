#!/bin/bash

# Migration Script for LaundrySignalR
# Migrates subjects from JSON file to MongoDB

echo "🚀 Starting LaundrySignalR Migration..."
echo "=====================================/"

# Check if application is running
if ! curl -s http://localhost:5263/health > /dev/null 2>&1; then
    echo "⚠️  Application is not running. Starting it now..."
    echo "   Run this in another terminal: dotnet run"
    echo "   Then run this script again."
    exit 1
fi

echo "✅ Application is running"

# Check migration status first
echo ""
echo "📊 Checking current migration status..."
MIGRATION_STATUS=$(curl -s http://localhost:5263/api/migration/status?tenantCode=default)
echo "Current Status: $MIGRATION_STATUS" | jq '.' 2>/dev/null || echo "$MIGRATION_STATUS"

echo ""
read -p "🤔 Do you want to proceed with migration? (y/N): " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Migration cancelled."
    exit 0
fi

# Perform migration
echo ""
echo "🔄 Migrating subjects from JSON to MongoDB..."
MIGRATION_RESULT=$(curl -s -X POST http://localhost:5263/api/migration/migrate-subjects?tenantCode=default)

echo "Migration Result:"
echo "$MIGRATION_RESULT" | jq '.' 2>/dev/null || echo "$MIGRATION_RESULT"

# Check if migration was successful
if echo "$MIGRATION_RESULT" | jq -e '.success == true' > /dev/null 2>&1; then
    echo ""
    echo "🎉 Migration completed successfully!"
    
    # Show final status
    echo ""
    echo "📊 Final status:"
    FINAL_STATUS=$(curl -s http://localhost:5263/api/migration/status?tenantCode=default)
    echo "$FINAL_STATUS" | jq '.' 2>/dev/null || echo "$FINAL_STATUS"
    
    echo ""
    echo "✅ Your subjects are now in MongoDB!"
    echo "🧪 Test your API:"
    echo "   curl http://localhost:5263/api/subjects"
    
else
    echo ""
    echo "❌ Migration failed. Check the error details above."
    echo "💡 You can retry by running this script again."
    exit 1
fi