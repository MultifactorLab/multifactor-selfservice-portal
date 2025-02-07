[![Build status](https://ci.appveyor.com/api/projects/status/p285up9ctsvo2p13?svg=true)](https://ci.appveyor.com/project/MultifactorLab/multifactor-selfservice-portal)
<!-- ![CodeQL](https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal/workflows/CodeQL/badge.svg) -->

# MultiFactor.SelfService.Linux.Portal
_Also available in other languages: [Русский](README.ru.md)_

MultiFactor SelfService Portal (Linux version) is a website developed and maintained by MultiFactor for self-enrollment of a second authentication factor by users within an `Active Directory` corporate network. In addition to Active Directory other ldap directories are also supported.

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
- Supports Active Directory and other ldap directories;
- Supports captcha verification before user's login.

The portal is designed to be installed and operated within the corporate network perimeter.
> :warning: The Linux version of the MultiFactor SelfService Portal does not support Cyrillic passwords. 

## First Steps

1. Navigate to your Multifactor <a href="https://multifactor.pro/login" target="_blank">personal profile</a> , and under the **Resources** section create a new website with the following paramteres:
  - `Name and address:` `<could be any>`;
  - `Token format:` `JwtRS256`.
2. Upon creation you should receive **ApiKey** and **ApiSecret** parameters. You will need these parameters to complete the installation.

## Prerequisites

- application is installed on a Linux server, tested on Debian;
- the server with the installed portal requires access to the `api.multifactor.ru` host via TCP port 443 (TLS). 

## Configuration

Portal settings are stored in the `appsettings.production.xml` file in XML format.

```xml
<PortalSettings>

	<CompanySettings>
		<Name>ACME</Name>
		<Domain>ldaps://dc.domain.local/dc=domain,dc=local</Domain>
		<LogoUrl>images/logo.svg</LogoUrl>
	</CompanySettings>

	<TechnicalAccountSettings>
		<User>user</User>
		<Password>password</Password>
	</TechnicalAccountSettings>
	
	<!-- requiresUserPrincipalName: Only UPN user name format permitted -->
	<ActiveDirectorySettings requiresUserPrincipalName="false">
		<!--<SecondFactorGroup>2FA Users</SecondFactorGroup>-->
		<!--<UseUserPhone>true</UseUserPhone>-->
		<!--<UseMobileUserPhone>true</UseMobileUserPhone>-->
	</ActiveDirectorySettings>

	<MultiFactorApiSettings>
		<ApiUrl>https://api.multifactor.ru</ApiUrl>
		<ApiKey>key</ApiKey>
		<ApiSecret>secret</ApiSecret>
		<!--<ApiProxy>http://proxy:3128</ApiProxy>-->
	</MultiFactorApiSettings>

	<CaptchaSettings enabled="false">
		<!--Captcha provider: 'Google', 'Yandex'-->
		<CaptchaType>Google</CaptchaType>
		<Key>key</Key>
		<Secret>secret</Secret>
		<!--When Captcha is displayed: 'Always', 'PasswordRecovery' -->
		<CaptchaRequired>Always</CaptchaRequired>
	</CaptchaSettings>

	<!--<LoggingLevel>Info</LoggingLevel>-->
	<!--<LoggingFormat>json</LoggingFormat>-->

	<!-- Enable user password change. AD connection must be secure (SSL/TLS) -->
	<!-- To Enable password recovery, Captcha on PasswordRecovery page must be enabled -->
	<PasswordManagement enabled="false" allowPasswordRecovery="false">
		<!-- Changing session duration in hh:mm:ss (00:02:00 by default) -->
		<!-- <PasswordChangingSessionLifetime>00:02:00</PasswordChangingSessionLifetime> -->
		<!-- Session storage size in `bytes` (5242880 by default, 1048576 is minimal value) -->
		<!-- <PasswordChangingSessionCachesize>5242880</PasswordChangingSessionCachesize> -->
	</PasswordManagement>

	<!-- Enable user Exchange AciveSync devices provisioning. Don't works with Samba. -->
	<ExchangeActiveSyncDevicesManagement enabled="false" />

	<!--<UICulture>auto:en</UICulture>-->

	<!--<GroupPolicyPreset>-->
		<!-- Groups to assign to the registered user -->
		<!--<SignUpGroups>group name 1;group 2</SignUpGroups> -->
	<!--</GroupPolicyPreset>-->

	<!--<LdapBaseDn></LdapBaseDn>-->
</PortalSettings>
```
If the `ActiveDirectorySettings.UseUserPhone` option is enabled, the component will use the phone stored in the **General** tab. All phone number formats are supported.

<img src="https://multifactor.pro/img/radius-adapter/ra-ad-phone-source.png" width="400">

If the `ActiveDirectorySettings.UseMobileUserPhone` option is enabled, the component will use the phone stored in the **Telephones** tab in the **Mobile** field. All phone number formats are supported.

<img src="https://multifactor.pro/img/radius-adapter/ra-ad-mobile-phone-source.png" width="400">

## Installation (Debian 12, Nginx)
This is a web application. The Kestrel web server is used to launch it. The web server does not need to be installed and is included in the <a href="https://github.com/MultifactorLab/multifactor-selfservice-portal/releases" target="_blank">release</a>. 
A typical scheme for running .NET 8 web application on the Linux server is as follows:
1. The application is deployed to the Kestrel web server. The Kestrel listens for requests on a specific port (5000) of the local host using the `http` scheme.
2. The reverse proxy configured on the Linux server listens to requests on the server's external port using the `https` scheme and redirects them to the local port 5000.
3. The Kestrel web server processes incoming requests and sends them to the web application.

Therefore, the Portal application is behind a reverse proxy and processes requests only from it.

### 1. Setup environment
The application requires .NET 8 runtime packages.  
> More information <a href="https://docs.microsoft.com/en-us/dotnet/core/install/linux#microsoft-packages" target="_blank">here</a>.

Run the following commands to add the Microsoft package signing key to your list of trusted keys and add the package repository:
```
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
``` 
Install the runtime:
```
sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-8.0
```  
Create directories:
```
sudo mkdir /opt/multifactor /opt/multifactor/ssp /opt/multifactor/ssp/app
sudo mkdir /opt/multifactor/ssp/logs /opt/multifactor/ssp/key-storage
```
### 3. Copy files
Download and extract application files:
```
sudo wget https://github.com/MultifactorLab/multifactor-selfservice-portal/releases/latest/download/MultiFactor.SelfService.Linux.Portal.zip

sudo unzip MultiFactor.SelfService.Linux.Portal.zip -d $app_dir
```
Create a user and set up permissions:
```
sudo useradd mfa

sudo chown -R mfa: /opt/multifactor/ssp
sudo chmod -R 700 /opt/multifactor/ssp
```

### 4. Configure Nginx
```
sudo apt-get install nginx
sudo service nginx start
```
Check that the browser displays the default landing page for Nginx `http://<server_IP_address>/index.nginx-debian.html`.

Configure Nginx as a reverse proxy. Open file:
```
sudo vi /etc/nginx/sites-available/default
```
Replace the contents with the following snippet:
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
	server_name ssp.domain.org;

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
  ssl_certificate /etc/letsencrypt/live/ssp.domain.org/fullchain.pem;
  ssl_certificate_key /etc/letsencrypt/live/ssp.domain.org/privkey.pem;
  include /etc/letsencrypt/options-ssl-nginx.conf;
  ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;

}

# default server to redirect http -> https
server {
  if ($host = ssp.domain.org) {
      return 301 https://$host$request_uri;
  }

  listen 80;

  server_name domain.org;
  return 404;
}
```
In this case SSL have been configured using service <a href="https://letsencrypt.org/" target="_blank">Let's Encrypt</a> and `certbot` package. Follow <a href="https://www.nginx.com/blog/using-free-ssltls-certificates-from-lets-encrypt-with-nginx/" target="_blank">this</a> instruction to get similar settings.


### 5. Create the systemd service
Create the service definition file:
```
sudo vi /etc/systemd/system/ssp.service
```
```
[Unit]
Description=Self Service Portal

[Service]
WorkingDirectory=/opt/multifactor/ssp/app
ExecStart=/usr/bin/dotnet /opt/multifactor/ssp/app/MultiFactor.SelfService.Linux.Portal.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
TimeoutStopSec=90
SyslogIdentifier=ssp-service
User=mfa
Environment=ASPNETCORE_ENVIRONMENT=production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Save the file and enable the service:
```
sudo systemctl enable ssp.service
```
Start the service and verify that it's running:
```
sudo systemctl start ssp.service
sudo systemctl status ssp.service
```
At any point in the future after changing the service file, trload the systemd manager configuration and restart service:
```
sudo systemctl daemon-reload
sudo systemctl restart ssp.service
```

## Logs

The Self-Service Portal logs are located in `/opt/multifactor/ssp/logs` directory. If they are not there, make sure that the directory is writable by the sspl-service-user. Logs are also saved to `syslog`.   

To view the syslog use this command: 
```
less /var/log/syslog
```

To view the ssp.service logs use this command:
```
sudo journalctl -fu ssp.service
```

Logging can be provided in json:

```
<LoggingFormat>format</LoggingFormat>
```

It's possible to choose ```format``` from predefined formats. Here are possible values of the ```format``` parametr (register is not case-sensitive).

* ```Json``` or ```JsonUtc```. Compact logging, times in UTC.

   ```json
   {"@t":"2016-06-07T03:44:57.8532799Z","@m":"Hello, \"nblumhardt\"","@i":"7a8b9c0d","User":"nblumhardt"}
   ```

* ```JsonTz```. Compact logging, differs from ```JsonUtc``` by the time format. In this kind of format the local time with time zone is indicated.

  ```Json
   {"@t":"2023-11-23 17:16:29.919 +03:00","@m":"Hello, \"nblumhardt\"","@i":"7a8b9c0d","User":"nblumhardt"}
   ```

* ```Ecs```. Ecs formats logs according to elastic common schema.

   ```json
   {
     "@timestamp": "2019-11-22T14:59:02.5903135+11:00",
     "log.level": "Information",
     "message": "Log message",
     "ecs": {
       "version": "1.4.0"
     },
     "event": {
       "severity": 0,
       "timezone": "AUS Eastern Standard Time",
       "created": "2019-11-22T14:59:02.5903135+11:00"
     },
     "log": {
       "logger": "Elastic.CommonSchema.Serilog"
     },
     "process": {
       "thread": {
         "id": 1
       },
       "executable": "System.Threading.ExecutionContext"
     }
   }
   ```

## Access Portal

The portal can be accessed at `https://yourdomain.com/mfa`

For liveness check use GET https://ssp.domain.org/api/ping.  
Response example:
```json
{
    "timeStamp": "2022-08-05T08:19:42.336Z",
    "message": "Ok"
}
```


## LDAP implementations supporting
Self Service Portal has been tested with the following implementations:
 - ActiveDirectory
 - Samba4
 - FreeIPA
### FreeIPA
For the correct connection you need to set `LdapBaseDn` setting in the configuration file. Example:
```xml
<LdapBaseDn>cn=users,cn=accounts,dc=domain,dc=local</LdapBaseDn>
```


## Use Cases

The portal is used for self-enrollment and registration of the second authentication factor by users within the corporate network. It also acts as a Single Sign-On entry point for corporate SSO applications.

Once the second-factor is configured, users can securely connect via VPN, VDI, or Remote Desktop.

- Two Factor Authentication [Windows VPN with Routing and Remote Access Service (RRAS)](https://multifactor.pro/docs/windows-2fa-rras-vpn/)
- Two-factor authentication for [Microsoft Remote Desktop Gateway](https://multifactor.pro/docs/windows-2fa-remote-desktop-gateway/)

## License

MultiFactor SelfService Portal is distributed under the [MIT License](LICENSE.md).
The component is a part of <a href="https://multifactor.pro/" target="_blank">MultiFactor</a> 2FA hybrid solution.