#!/bin/bash
set -e

# Auto-detect dotnet
if ! command -v dotnet &> /dev/null; then
    if [ -f "/usr/local/share/dotnet/dotnet" ]; then
        export PATH=$PATH:/usr/local/share/dotnet
    elif [ -f "/opt/homebrew/share/dotnet/dotnet" ]; then
        export PATH=$PATH:/opt/homebrew/share/dotnet
    elif [ -f "$HOME/.dotnet/dotnet" ]; then
        export PATH=$PATH:$HOME/.dotnet
    else
        echo "dotnet could not be found. Please install .NET 8 SDK."
        exit 1
    fi
fi

# Configuration
APP_NAME="mac-iap"
PROJECT_DIR="IapDesktop.Application.Avalonia"
PUBLISH_DIR="bin/Release/net8.0/osx-arm64/publish"
BUNDLE_DIR="$APP_NAME.app"
CONTENTS_DIR="$BUNDLE_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"

echo "Building $APP_NAME for macOS (ARM64)..."

# 1. Publish the application
dotnet publish "$PROJECT_DIR" -c Release -r osx-arm64 --self-contained -p:UseAppHost=true

# 2. Create App Bundle Structure
echo "Creating App Bundle structure..."
rm -rf "$BUNDLE_DIR"
mkdir -p "$MACOS_DIR"
mkdir -p "$RESOURCES_DIR"

# 3. Copy Assets
echo "Copying executables..."
cp -a "$PROJECT_DIR/$PUBLISH_DIR/." "$MACOS_DIR/"

echo "Copying native libraries..."
find "$PROJECT_DIR/$PUBLISH_DIR" -name "*.dylib" -exec cp {} "$MACOS_DIR/" \;

echo "Copying Info.plist..."
cp "$PROJECT_DIR/Info.plist" "$CONTENTS_DIR/"

# 4. Copy Icon (if exists)
if [ -f "$PROJECT_DIR/Assets/AppIcon.icns" ]; then
    echo "Copying AppIcon.icns..."
    cp "$PROJECT_DIR/Assets/AppIcon.icns" "$RESOURCES_DIR/"
else
    echo "WARNING: AppIcon.icns not found in Assets/!"
fi

# 5. Cleanup and Permission
echo "Cleaning up file permissions..."
chmod +x "$MACOS_DIR/IapDesktop.Application.Avalonia"

echo "-----------------------------------"
echo "Build Complete: $BUNDLE_DIR"
echo "To run: open $BUNDLE_DIR"
