# 🔐 Security Configuration Checklist

Use this checklist to ensure your MongoDB credentials are properly secured.

## ✅ Development Setup Complete

- [x] MongoDB connection string stored in User Secrets
- [x] Configuration templates created (`.template` files)
- [x] Sensitive files added to `.gitignore`
- [x] Setup script created for team members
- [x] Application tested and working

## 📋 Before Committing Code

### Check Git Status
```bash
# Verify these files are NOT staged for commit:
git status

# Should NOT see:
# - appsettings.json
# - appsettings.Development.json  
# - Any files with actual credentials

# Should see (OK to commit):
# - appsettings.json.template
# - appsettings.Development.json.template
# - CONFIGURATION.md
# - setup-dev.sh
```

### Verify Secrets are Hidden
```bash
# Check that credentials are not in any committed files
git log --oneline -10
git show HEAD  # Should not contain any MongoDB credentials

# Check current files that would be committed
git diff --cached
```

## 🚀 Deployment Checklist

### Development
- [x] User Secrets configured: `dotnet user-secrets list`
- [x] Application starts: `dotnet run`
- [x] Health check works: `curl http://localhost:5263/health`

### Production (Choose One)

#### Environment Variables
- [ ] `MONGODB_CONNECTION_STRING` set in hosting platform
- [ ] `MONGODB_DATABASE_NAME` set in hosting platform  
- [ ] Application deployed and tested

#### Configuration File (Less Secure)
- [ ] appsettings.Production.json created on server only
- [ ] File permissions restricted (600)
- [ ] File excluded from backups/logs
- [ ] Credentials rotated after setup

## 🔍 Security Verification

### Check for Credential Leaks
```bash
# Search for potential credential leaks in git history
git log --all --full-history -- appsettings*.json

# Search for MongoDB credentials in files
grep -r "mongodb+srv" . --exclude-dir=.git

# Check User Secrets are being used (should show your connection)
dotnet user-secrets list
```

### Test Multi-Environment Setup
```bash
# Development should use User Secrets
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Production should use Environment Variables
ASPNETCORE_ENVIRONMENT=Production MONGODB_CONNECTION_STRING="test" dotnet run
```

## 🛡️ Additional Security Measures

### MongoDB Atlas Security
- [ ] Database user has minimal required permissions
- [ ] IP whitelist configured (production)
- [ ] Connection encryption enabled (TLS)
- [ ] Database access logs enabled
- [ ] Regular security audits scheduled

### Application Security  
- [ ] HTTPS enabled in production
- [ ] CORS properly configured
- [ ] Request logging enabled (but credentials filtered)
- [ ] Error handling doesn't expose credentials
- [ ] Regular dependency updates scheduled

### Team Security
- [ ] Team members trained on secrets management
- [ ] Code review process includes credential checks  
- [ ] CI/CD pipeline checks for credential leaks
- [ ] Incident response plan for credential exposure

## 🚨 If Credentials are Accidentally Committed

### Immediate Actions
1. **Rotate credentials immediately**
   - Change MongoDB password
   - Update User Secrets: `dotnet user-secrets set "MongoDB:ConnectionString" "new-connection"`
   - Update production environment variables

2. **Clean Git history** (if recent)
   ```bash
   # Remove file from last commit
   git reset --soft HEAD~1
   git reset HEAD appsettings.json
   git commit -m "Remove sensitive configuration"
   
   # Force push (⚠️ Only if no one else pulled the commit)
   git push --force-with-lease
   ```

3. **Notify team and security**
   - Inform all team members
   - Check access logs for suspicious activity
   - Document incident for future prevention

### Prevention
- Use pre-commit hooks to scan for secrets
- Regular security training
- Automated secret scanning in CI/CD

## ✨ Best Practices Summary

1. **Never commit real credentials to git**
2. **Use User Secrets for development**
3. **Use Environment Variables for production**  
4. **Provide templates for team setup**
5. **Regular credential rotation**
6. **Monitor and audit access**
7. **Team education and training**

## 🔗 Useful Commands

```bash
# User Secrets management
dotnet user-secrets init
dotnet user-secrets set "key" "value"
dotnet user-secrets list
dotnet user-secrets remove "key"
dotnet user-secrets clear

# Git credential checking
git log --oneline --all -- "*.json"
git show HEAD -- appsettings.json

# Application testing
dotnet run
curl http://localhost:5263/health
```