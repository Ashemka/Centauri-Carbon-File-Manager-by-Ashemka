# Contributing to Centauri Carbon Downloader

Thank you for your interest in contributing! This document explains how to contribute to the project.

## Code of Conduct

- Be respectful and professional
- Focus on the code, not the person
- No harassment, discrimination, or hate speech
- Help others learn and grow

## How to Contribute

### 1. Fork and Clone

```bash
# Fork the repository on GitHub
# Then clone your fork
git clone https://github.com/YOUR-USERNAME/Centauri-Carbon-File-Manager-by-Ashemka.git
cd Centauri-Carbon-File-Manager-by-Ashemka
```

### 2. Create a Branch

```bash
# Always create a feature branch from main
git checkout -b feature/your-feature-name
# or for bug fixes
git checkout -b fix/bug-description
```

**Branch naming convention:**
- `feature/short-description` for new features
- `fix/bug-description` for bug fixes
- `docs/update-description` for documentation
- `refactor/area-name` for refactoring

### 3. Make Your Changes

- Write clear, readable code
- Follow C# conventions and .NET standards
- Keep methods focused and single-purpose
- Add comments for complex logic

### 4. Commit Message Format

Write clear commit messages:

```
Short summary (50 chars max)

Detailed explanation if needed. Explain WHAT and WHY, not HOW.
Keep lines under 72 characters.

- Bullet point 1
- Bullet point 2
```

Good examples:
- `Add automatic FFmpeg download on startup`
- `Fix WebSocket timeout on slow connections`
- `Improve dark mode colors for accessibility`

Bad examples:
- `Fixed stuff`
- `WIP`
- `asdf`

### 5. Test Your Changes

Before submitting:

```bash
# Restore dependencies
dotnet restore

# Build in Debug mode
dotnet build -c Debug

# Or use the build script
build_release_win64.bat
```

Test the application:
- Test on Windows 10/11
- Test both light and dark themes
- Test with different languages
- Try edge cases

### 6. Push and Create a Pull Request

```bash
# Push your branch
git push origin feature/your-feature-name
```

Then on GitHub:
- Click "New Pull Request"
- Fill in a clear title and description
- Reference any related issues

**Pull Request Template:**

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] New feature
- [ ] Bug fix
- [ ] Documentation update
- [ ] Refactoring

## Testing Done
Describe how you tested these changes

## Screenshots (if applicable)
Add UI changes screenshots

## Checklist
- [ ] Code follows style conventions
- [ ] Self-reviewed the changes
- [ ] Tested on Windows 10/11
- [ ] Updated documentation if needed
- [ ] License header included in new files
```

## Development Setup

### Prerequisites
- Windows 10/11 (x64)
- .NET 8.0 SDK or later
- Git
- Visual Studio or VS Code (optional)

### First Run

```bash
# Clone and enter directory
git clone https://github.com/Ashemka/Centauri-Carbon-File-Manager-by-Ashemka.git
cd Centauri-Carbon-File-Manager-by-Ashemka

# Restore packages
dotnet restore

# Build Debug version
dotnet build -c Debug

# Run
dotnet run --configuration Debug --project CentauriCarbonDownloader.csproj
```

## Coding Style

### C# Conventions

```csharp
// Classes and methods: PascalCase
public class MyClass
{
    public void MyMethod()
    {
        // Code
    }
}

// Private fields: camelCase
private string _myField;
private int _counter = 0;

// Local variables: camelCase
string localVariable = "value";

// Constants: PascalCase
const int MaxRetries = 3;
```

### Naming Guidelines
- Use meaningful, descriptive names
- Avoid single-letter variables (except loops: i, j, x, y)
- Use full words, not abbreviations
- `GetFileList()` not `GetFL()`

### File Organization
- One class per file
- Filename matches class name
- Proper namespace matching folder structure

## Areas to Contribute

### High Priority
- Bug fixes and stability improvements
- Performance optimizations
- Documentation improvements
- Accessibility improvements

### Medium Priority
- New language support
- UI/UX improvements
- Test coverage

### Lower Priority
- Cosmetic changes
- Code refactoring

## Reporting Issues

Found a bug? Please report it on GitHub:

1. Check if the issue already exists
2. Use a clear, descriptive title
3. Include:
   - Windows version
   - .NET SDK version
   - Steps to reproduce
   - Expected vs actual behavior
   - Screenshots if applicable
   - Logs from `%LOCALAPPDATA%\CentauriCarbonDownloader\`

## Feature Requests

Have an idea? Open a GitHub Discussion or Issue:

1. Describe the feature
2. Explain the use case
3. Provide examples
4. Be open to feedback

## License

By contributing, you agree that your contributions will be licensed under the project's license (non-commercial use with attribution).

## Questions?

- Open a GitHub Discussion
- Check existing issues
- Review the documentation

Thank you for contributing! 🎉
