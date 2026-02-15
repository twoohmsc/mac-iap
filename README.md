# IAP for macOS 🐧💻

IAP for macOS is a professional Remote Desktop and SSH client that provides secure, zero-trust access to your Google Cloud VM instances from anywhere.

[![GitHub Release](https://img.shields.io/github/v/release/GoogleCloudPlatform/iap-desktop?label=Latest%20Release)](https://github.com/GoogleCloudPlatform/iap-desktop/releases/latest)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE.txt)

---

> [!NOTE]
> This project is a **macOS-focused port** and fork of the original [IAP Desktop](https://github.com/GoogleCloudPlatform/iap-desktop) for Windows.
> 
> **Special Thanks** to the original [IAP Desktop team at Google](https://github.com/GoogleCloudPlatform/iap-desktop/graphs/contributors) for their incredible work on the core logic and security framework that made this port possible! 🚀

---

## 🚀 Key Features

### 🛡️ Zero-Trust Security (IAP)
IAP for macOS uses [Identity-Aware-Proxy (IAP)](https://cloud.google.com/iap/docs/tcp-forwarding-overview) to provide secure access without needing public IP addresses.
*   **Context-Aware Access**: Define who can access which VM based on identity and device health.
*   **No Public IPs**: Keep your VMs hidden from the public internet.
*   **Automatic Tunnels**: Seamlessly manages IAP TCP forwarding tunnels for you.

### 🐧 Secure SSH & Interactive Terminal
Connect to Linux VMs with a built-in, high-performance SSH terminal.
*   **Rich Terminal Experience**: Interactive terminal with ANSI escape sequence support and smooth input handling.
*   **OS Login Support**: Integration with [OS Login](https://cloud.google.com/compute/docs/oslogin) for managing SSH keys and 2FA.
*   **Ephemeral Credentials**: Automatically generates and publishes SSH keys for quick access.

### ☁️ Multi-Project VM Management
A consolidated Project Explorer for all your Google Cloud resources.
*   **Hierarchical View**: Organize VMs by Project, Zone, and Instance.
*   **Visual Mascot Icons**: Instantly identify Linux VMs with the official Tux mascot (🐧) and Windows VMs with the 🪟 icon.
*   **Multi-Project Support**: Manage and switch between multiple projects with ease.

### 📂 Integrated SFTP & File Management
Manage your files on remote instances without leaving the app.
*   **SFTP Browser**: Navigate remote file systems with a familiar UI.
*   **Upload/Download**: Fast and secure file transfers over the same IAP protected connection.

---

##  macOS Port Highlights
The macOS port is built using **Avalonia UI**, providing a native look and feel while leveraging the robust core logic of the original IAP Desktop.

*   **Native macOS Integration**: Optimized for macOS window management and dock behavior.
*   **InterFont Support**: Modern, clean typography for better readability.
*   **libssh2 Integration**: Leverages native `libssh2` for high-performance SSH operations.

---

## 🛠️ Getting Started

### Prerequisites
*   **macOS** (Intel or Apple Silicon)
*   [gcloud CLI](https://cloud.google.com/sdk/gcloud) installed and authenticated (`gcloud auth login`)
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for building from source)

### Building and Running
```bash
# Clone the repository
git clone https://github.com/GoogleCloudPlatform/iap-desktop.git
cd iap-desktop/mac-iap/mac-iap-port/IapDesktop.Application.Avalonia

# Build and Run
dotnet run
```

---

## 🤝 Contributing
IAP for macOS is an open-source project. We welcome contributions, bug reports, and feature requests!

_IAP for macOS is an open-source project developed and maintained by the Google Cloud Solutions Architects team. The project is not an officially supported Google product._

_All files in this repository are under the [Apache License, Version 2.0](LICENSE.txt) unless noted otherwise._
