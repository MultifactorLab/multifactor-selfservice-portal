
[![Build status](https://ci.appveyor.com/api/projects/status/p285up9ctsvo2p13?svg=true)](https://ci.appveyor.com/project/MultifactorLab/multifactor-selfservice-portal)
<!-- ![CodeQL](https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal/workflows/CodeQL/badge.svg) -->  

# MultiFactor self-service portal
_Also available in other languages: [Русский](README.ru.md)_

## What is MultiFactor self-service portal?
MultiFactor SelfService Portal (Linux version) is a website developed and maintained by MultiFactor for self-enrollment of a second authentication factor by users within an `Active Directory` corporate network. In addition to Active Directory other ldap directories are also supported.

The portal is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution.

* <a href="https://github.com/MultifactorLab/multifactor-selfservice-portal" target="_blank">Source code</a>
* <a href="https://github.com/MultifactorLab/multifactor-selfservice-portal/releases" target="_blank">Releases</a>

## Table of Contents

- [What is MultiFactor self-service portal](#what-is-multiFactor-self-service-portal)
- [Component Features](#component-features)
- [Use Cases](#use-cases)
- [Installation and configuration](#installation-and-configuration)
- [License](#license)

## Component Features

- User's login and password verification in an Active Directory domain. Multiple domains are supported if a trust relationship is configured between them;
- Configuration of the second authentication factor by the end-user;
- User's password change (available only after the second-factor confirmation);
- Single Sign-On for corporate applications;
- Supports Active Directory and other ldap directories;
- Supports captcha verification before user's login.

The portal is designed to be installed and operated within the corporate network perimeter.

## Use Cases

The portal is used for self-enrollment and registration of the second authentication factor by users within the corporate network. It also acts as a Single Sign-On entry point for corporate SSO applications.

Once the second-factor is configured, users can securely connect via VPN, VDI, or Remote Desktop.

- Two Factor Authentication [Windows VPN with Routing and Remote Access Service (RRAS)](https://multifactor.pro/docs/windows-2fa-rras-vpn/)
- Two-factor authentication for [Microsoft Remote Desktop Gateway](https://multifactor.pro/docs/windows-2fa-remote-desktop-gateway/)

## Installation and configuration
See [knowledge base](https://multifactor.pro/docs/multifactor-selfservice-linux-portal/) for information about configuration, launch and an additional guidance.

## License

MultiFactor SelfService Portal is distributed under the [MIT License](LICENSE.md).  
The component is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution.