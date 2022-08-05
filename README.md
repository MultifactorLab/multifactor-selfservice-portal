[![Build status](https://ci.appveyor.com/api/projects/status/p285up9ctsvo2p13?svg=true)](https://ci.appveyor.com/project/MultifactorLab/multifactor-selfservice-portal)
<!-- ![CodeQL](https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal/workflows/CodeQL/badge.svg) -->

# MultiFactor.SelfService.Linux.Portal
_Also available in other languages: [Русский](README.ru.md)_

MultiFactor SelfService Portal is a website developed and maintained by MultiFactor for self-enrollment of a second authentication factor by users within an `Active Directory` corporate network. In addition to Active Directory other ldap directories are also supported.

The portal is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution.

* <a href="https://github.com/MultifactorLab/multifactor-selfservice-portal" target="_blank">Source code</a>
* <a href="https://github.com/MultifactorLab/multifactor-selfservice-portal/releases" target="_blank">Build</a>

See documentation at https://multifactor.pro/docs/multifactor-selfservice-linux-portal/ for additional guidance on  Self-Service Portal deployment.

## Table of Contents

- [Features](#features)
- [First Steps](#first-steps)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Installation](#installation)
- [Logs](#logs)
- [Access Portal](#access-portal)
- [Use Cases](#use-cases)
- [License](#license)

## Features

- User's login and password verification in an Active Directory domain. Multiple domains are supported if a trust relationship is configured between them;
- Configuration of the second authentication factor by the end-user;
- User's password change (available only after the second-factor confirmation);
- Single Sign-On for corporate applications;
- Supports Active Directory and other ldap directories.

The portal is designed to be installed and operated within the corporate network perimeter.

## First Steps

1. Navigate to your Multifactor <a href="https://multifactor.pro/login" target="_blank">personal profile</a> , and under the **Resources** section create a new website with the following paramteres:
  - `Name and address:` `<could be any>`;
  - `Token format:` `JwtRS256`.
2. Upon creation you should receive **ApiKey** and **ApiSecret** parameters. You will need these parameters to complete the installation.

## Prerequisites

- installation requires Linux server;
- the server with the installed portal requires access to the `api.multifactor.ru` host via TCP port 443 (TLS);
- '.NET 6 runtime' installed on the server;
- `libldap-2.4-2` package istalled on the server;
- any reverse proxy server installed and configured on the server;
- unix service file configured using `systemd`;
- technical unix user added (e.g. sspl-service-user);
- directories `/var/sspl-key-storage, /var/www/logs` created on the server;
- directories from prev item must be owned by technical unix user. 

## Configuration

Portal settings are stored in the `appsettings.production.xml` file in XML format.

```xml
<PortalSettings>			
				
    <!-- Name of your organization -->
    <CompanyName>ACME</CompanyName>
    <!-- Name of your Active Directory domain to verify username and password -->
    <CompanyDomain>ldaps://dc.domain.local/dc=domain,dc=local</CompanyDomain>     
    <!-- Company logo URL address -->
    <CompanyLogoUrl>images/logo.svg</CompanyLogoUrl>

    <!-- Technical Active Directory account -->
    <TechnicalAccUsr>user</TechnicalAccUsr>
    <TechnicalAccPwd>password</TechnicalAccPwd>

    <!-- [Optional] Require second factor for users in specified group only (Single Sign-On users). Second-factor will be required for all users by default if setting is deleted. -->
    <!--<ActiveDirectory2faGroup>2FA Users</ActiveDirectory2faGroup>-->
			
    <!-- [Optional] Use your users' phone numbers contained in Active Directory to automatically enroll your users and start send one-time SMS codes. Option is not used if settings are removed. -->
    <!--<UseActiveDirectoryUserPhone>true</UseActiveDirectoryUserPhone>-->    
    <!--<UseActiveDirectoryMobileUserPhone>true</UseActiveDirectoryMobileUserPhone>-->
    
    <!-- Use UPN username -->
    <!--<RequiresUserPrincipalName>true</RequiresUserPrincipalName>-->

    <!-- Multifactor API Address -->
    <MultifactorApiUrl>https://api.multifactor.ru</MultifactorApiUrl>
    <!-- API KEY parameter from the Multifactor personal account -->
    <MultifactorApiKey>key</MultifactorApiKey>
    <!-- API Secret parameter from the Multifactor personal account -->
    <MultifactorApiSecret>secret</MultifactorApiSecret>

    <!-- [Optional] Access the Multifactor API via the HTTP proxy -->
    <!--<MultifactorApiProxy>http://proxy:3128</MultifactorApiProxy>-->

    <!-- Logging level: 'Debug', 'Info', 'Warn', 'Error' -->
    <LoggingLevel>Info</LoggingLevel>

    <!-- Enable user password change. AD connection must be secure (SSL/TLS) -->
    <EnablePasswordManagement>true</EnablePasswordManagement>
    <!-- Enable user Exchange AciveSync devices provisioning. Not works with Samba. -->
    <EnableExchangeActiveSyncDevicesManagement>false</EnableExchangeActiveSyncDevicesManagement>

    <!--
       UI languahe selection:
       ru - Russian,
       en - English,
       auto:ru - check browser, default Russian,
       auto:en - check browser, default English.
       If option not specefied - English.
    -->
    <UICulture>auto:en</UICulture>
</PortalSettings>
```
If the `UseActiveDirectoryUserPhone` option is enabled, the component will use the phone stored in the **General** tab. All phone number formats are supported.

<img src="https://multifactor.pro/img/radius-adapter/ra-ad-phone-source.png" width="400">

If the `UseActiveDirectoryMobileUserPhone` option is enabled, the component will use the phone stored in the **Telephones** tab in the **Mobile** field. All phone number formats are supported.

<img src="https://multifactor.pro/img/radius-adapter/ra-ad-mobile-phone-source.png" width="400">

## Installation
This is a web application. The Kestrel web server is used to launch it. The web server does not need to be installed and is included in the <a href="https://github.com/MultifactorLab/multifactor-selfservice-portal/releases" target="_blank">relese</a>. 
A typical scheme for running .NET 6 web application on the Linux server is as follows:
1. The application is deployed to the Kestrel web server. The Kestrel listens for requests on a specific port (5000) of the local host using the `http` scheme.
2. The reverse proxy configured on the Linux server listens to requests on the server's external port using the `https` scheme and redirects them to the local port 5000.
3. The Kestrel web server processes incoming requests and sends them to the web application.

Therefore, the Portal application is behind a reverse proxy and processes requests only from it.

> For naming directories, services, etc. the name `sspl` will be used. You can choose another.

### 1. Setup environment
The application requires .NET 6 runtime packages.  
> More information <a href="https://docs.microsoft.com/en-us/dotnet/core/install/linux#microsoft-packages" target="_blank">here</a>.

Run the following commands to add the Microsoft package signing key to your list of trusted keys and add the package repository:
```
wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
``` 
Install the runtime:
```
sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-6.0
```  
Install libldap package:
```
sudo apt-get install libldap-2.4-2
```
Add technical user `sspl-service-user` (or choose another name).

### 3. Copy the app files
Copy <a href="https://github.com/MultifactorLab/multifactor-selfservice-portal/releases" target="_blank">this</a> application files in `/var/www/sspl/`.  
Make the user `sspl-service-user` the owner (recursively) of the /var/www/sspl.

### 4. Install and configure Nginx
Use this command to install Nginx:
```
sudo apt-get install nginx
```
Then start it:
```
sudo service nginx start
```
Check that the browser displays the default landing page for Nginx `http://<server_IP_address>/index.nginx-debian.html`.

To configure Nginx as a reverse proxy open `/etc/nginx/sites-available/default` in a text editor and replace the contents with the following snippet:
```
server {
  # Linux server DNS
  server_name sspl.domain.org;

  location / {
    # Kestrel http://host:port
    proxy_pass         http://localhost:5000;
    proxy_http_version 1.1;
    proxy_set_header   Upgrade $http_upgrade;
    proxy_set_header   Connection keep-alive;
    proxy_set_header   Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header   X-Forwarded-Proto $scheme;
  }

  listen 80;
}
```
Save file and verify Nginx configuration:
```
sudo nginx -t
```
Apply configuration:
```
sudo nginx -s reload
```

By default reverse proxy interacts with insecure http. If you need to install SSL certificate and setup https make sure that Nginx config looks like this: 
```
server {
	server_name sspl.domain.org;

	location / {
		proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
	}

  # listen port 443
  listen 443 ssl;
  # ssl configuration
  ssl_certificate /etc/letsencrypt/live/sspl.multifactor.dev/fullchain.pem;
  ssl_certificate_key /etc/letsencrypt/live/sspl.multifactor.dev/privkey.pem;
  include /etc/letsencrypt/options-ssl-nginx.conf;
  ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;

}

# default server to redirect http -> https
server {
  if ($host = sspl.domain.org) {
      return 301 https://$host$request_uri;
  }

  listen 80;

  server_name domain.org;
  return 404;
}
```
In this case SSL have been configured using service <a href="https://letsencrypt.org/" target="_blank">Let's Encrypt</a> and `certbot` package. Follow <a href="https://www.nginx.com/blog/using-free-ssltls-certificates-from-lets-encrypt-with-nginx/" target="_blank">this</a> instruction to get similar settings.


### 5. Create the systemd service
Create the definition file `/etc/systemd/system/sspl.service` with the following content:
```
[Unit]
Description=Self Service Portal for Linux Service

[Service]
WorkingDirectory=/var/www/sspl
ExecStart=/usr/bin/dotnet /var/www/sspl/MultiFactor.SelfService.Linux.Portal.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
TimeoutStopSec=90
SyslogIdentifier=sspl-service
User=sspl-service-user
Environment=ASPNETCORE_ENVIRONMENT=production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

In the preceding example, the user that manages the service is specified by the `User` option. The user (sspl-service-user) must exist and have proper recursively ownership of this directories: `/var/www/sspl`, `/var/sspl-key-storage`, `/var/www/logs`.

`Environment` option sets environment variable value. In this case `ASPNETCORE_ENVIRONMENT` variable value is `production`.

Other useful options:  
`RestartSec` – restart service after 10 seconds if the service crashes.  
`TimeoutStopSec` –  duration of time to wait for the app to shut down after it receives the initial interrupt signal.  
`SyslogIdentifier` – event log identifier.

Save the file and enable the service:
```
sudo systemctl enable sspl.service
```
Start the service and verify that it's running:
```
sudo systemctl start sspl.service
sudo systemctl status sspl.service
```
At any point in the future after changing the service file, trload the systemd manager configuration and restart service:
```
sudo systemctl daemon-reload
sudo systemctl restart sspl.service
```

## Logs

The Self-Service Portal logs are located in `/var/www/logs` directory. If they are not there, make sure that the directory is writable by the sspl-service-user. Logs are also saved to `syslog`.   

To view the syslog use this command: 
```
less /var/log/syslog
```

To view the sspl.service logs use this command:
```
sudo journalctl -fu sspl.service
```

## Access Portal

The portal can be accessed at `https://yourdomain.com/mfa`

For liveness check use GET https://sspl.domain.org/api/ping.  
Response example:
```json
{
    "environment": "production",
    "timeStamp": "2022-08-05T08:19:42.336Z",
    "version": "1.0.0",
    "apiStatus": "Ok",
    "ldapServicesStatus": "Ok"
}
```

## Use Cases

The portal is used for self-enrollment and registration of the second authentication factor by users within the corporate network. It also acts as a Single Sign-On entry point for corporate SSO applications.

Once the second-factor is configured, users can securely connect via VPN, VDI, or Remote Desktop.

- Two Factor Authentication [Windows VPN with Routing and Remote Access Service (RRAS)](https://multifactor.pro/docs/windows-2fa-rras-vpn/)
- Two-factor authentication for [Microsoft Remote Desktop Gateway](https://multifactor.pro/docs/windows-2fa-remote-desktop-gateway/)

## License

MultiFactor SelfService Portal is distributed under the [MIT License](LICENSE.md).
The component is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution.