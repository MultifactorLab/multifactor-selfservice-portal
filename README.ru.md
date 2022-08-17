[![Build status](https://ci.appveyor.com/api/projects/status/p285up9ctsvo2p13?svg=true)](https://ci.appveyor.com/project/MultifactorLab/multifactor-selfservice-portal)
<!-- ![CodeQL](https://github.com/MultifactorLab/MultiFactor.SelfService.Windows.Portal/workflows/CodeQL/badge.svg) -->

# MultiFactor.SelfService.Linux.Portal
_Also available in other languages: [English](README.md)_

MultiFactor SelfService Portal (версия для Linux) &mdash; веб-сайт, портал самообслуживания, разработанный и поддерживаемый компанией Мультифактор для самостоятельной регистрации второго фактора аутентификации пользователями внутри корпоративной сети `Active Directory`. Кроме Active Directory поддерживаются и другие ldap-каталоги.

Портал самообслуживания является частью гибридного 2FA решения сервиса <a href="https://multifactor.ru/" target="_blank">MultiFactor</a>.

* <a href="https://github.com/MultifactorLab/multifactor-selfservice-portal" target="_blank">Код</a>
* <a href="https://github.com/MultifactorLab/multifactor-selfservice-portal/releases" target="_blank">Сборка</a>

## Содержание

- [Функции портала](#функции-портала)
- [Первые шаги](#первые-шаги)
- [Требования для установки портала](#требования-для-установки-портала)
- [Конфигурация портала](#конфигурация-портала)
- [Установка портала](#установка-портала)
- [Журналы](#журналы)
- [Доступ к порталу](#доступ-к-порталу)
- [Сценарии использования](#сценарии-использования)
- [Лицензия](#лицензия)

## Функции портала

- Проверка логина и пароля пользователя в домене Active Directory, в том числе в нескольких доменах, если между ними настроены доверительные отношения;
- Настройка второго фактора аутентификации;
- Смена пароля пользователя после подтверждения второго фактора;
- Единая точка входа (Single Sign-On) для корпоративных приложений;
- Поддержка ActiveDirectory и других ldap-каталогов;
- Поддержка проверки капчи при входе в портал.

Портал предназначен для установки и работы внутри корпоративной сети.
> :warning:  Linux-версия MultiFactor SelfService Portal не поддерживает кириллические пароли. 

## Первые шаги

1. Зайдите в <a href="https://multifactor.ru/login" target="_blank">личный кабинет</a> Мультифактора, в разделе **Ресурсы** создайте новый веб-сайт:
  - название и адрес: `произвольные`;
  - формат токена: `JwtRS256`.
2. После создания вам будут доступны параметры **ApiKey** и **ApiSecret**, они потребуются для настройки портала.

## Требования для установки портала

- портал устанавливается на Linux сервер;
- серверу с установленным порталом необходим доступ к хосту `api.multifactor.ru` по TCP порту 443 (TLS).
- на сервере должна быть установлена среда выполнения `.NET 6 runtime`;
- должен быть правильным образом настроен реверс-прокси;
- для удобного управления процессом приложения должна быть настроена служба `systemd`;
- должен быть создан технический пользователь Linux (например, sspl-service-user);
- должны быть созданы дериктории  `/var/sspl-key-storage, /var/www/logs`;
- технический пользователь должен быть владельцем этих директорий.


## Конфигурация портала

Параметры работы портала хранятся в файле `appsettings.production.xml` в формате XML.

```xml
<PortalSettings>		

    <Company>		
        <!-- Название вашей организации -->
		<Name>ACME</Name>

		<!-- Название домена Active Directory для проверки логина и пароля пользователей -->
		<Domain>ldaps://dc.domain.local/dc=domain,dc=local</Domain>
			
		<!-- URL адрес логотипа организации -->
		<LogoUrl>images/logo.svg</LogoUrl>	
	</Company>

    <!-- Техническая учетная запись в Active Directory -->
    <TechnicalAccount>
		<User>user</User>
		<Password>password</Password>
	</TechnicalAccount

    <ActiveDirectoryOptions>
        <!--[Опционально] Запрашивать второй фактор только у пользователей из указанной группы для Single Sign On (второй фактор требуется всем, если удалить настройку) --> -->
		<!--<SecondFactorGroup>2FA Users</SecondFactorGroup>-->

		<!-- [Опционально] Использовать номер телефона из Active Directory для отправки одноразового кода в СМС (не используется, если удалить настройку). -->
		<!--<UseUserPhone>true</UseUserPhone>-->
		<!--<UseMobileUserPhone>true</UseMobileUserPhone>-->
	</ActiveDirectoryOptions>

    <MultiFactorApi>
		<!-- Адрес API Мультифактора -->
		<ApiUrl>https://api.multifactor.ru</ApiUrl>

		<!-- Параметр API KEY из личного кабинета Мультифактора. -->
		<ApiKey>key</ApiKey>

		<!-- Параметр API Secret из личного кабинета Мультифактора. -->
		<ApiSecret>secret</ApiSecret>

		<!-- [Опционально] Доступ к API Мультифактора через HTTP прокси.-->
		<!--<ApiProxy>http://proxy:3128</ApiProxy>-->
	</MultiFactorApi>

    <GoogleReCaptchaSettings>
		<!-- Включена проверка капчи Google reCaptcha2. -->
		<Enabled>false</Enabled>
			
		<!-- Site Key из личного кабинета https://www.google.com/recaptcha/admin -->
		<!--<Key>site key</Key>-->
			
		<!-- Secret Key из личного кабинета https://www.google.com/recaptcha/admin -->
		<!--<Secret>secret</Secret>-->
	</GoogleReCaptchaSettings>
    
    <!-- Использовать UPN для входа в портал -->
    <!--<RequiresUserPrincipalName>true</RequiresUserPrincipalName>-->

    <!-- Уровень логирования: 'Debug', 'Info', 'Warn', 'Error' -->
    <LoggingLevel>Info</LoggingLevel>

    <!-- Управление паролями. Необходимо подключение к AD по SSL/TLS -->
    <EnablePasswordManagement>true</EnablePasswordManagement>
    <!-- Управление устройствами Exchange AciveSync. Не работает для Samba. -->
    <EnableExchangeActiveSyncDevicesManagement>false</EnableExchangeActiveSyncDevicesManagement>

    <!--
        Язык интерфейса
        ru - Русский,
        en - English,
        auto:ru - язык браузера, по умолчанию Русский,
        auto:en - язык браузера, по умолчанию English.
        Если настройку убрать или оставить пустой, бует использоваться English.
        -->
    <UICulture>auto:ru</UICulture>
</PortalSettings>
```

При включении параметра `UseActiveDirectoryUserPhone` компонент будет использовать телефон, записанный на вкладке **General**. Формат телефона может быть любым.

<img src="https://multifactor.ru/img/radius-adapter/ra-ad-phone-source.png" width="400px">

При включении параметра `UseActiveDirectoryMobileUserPhone` компонент будет использовать телефон, записанный на вкладке **Telephones** в поле **Mobile**. Формат телефона также может быть любым.

<img src="https://multifactor.ru/img/radius-adapter/ra-ad-mobile-phone-source.png" width="400">

## Установка портала на примере Debian 11 и Nginx
Портал – это веб-приложение. Для запуска и работы портала используется веб-сервер Kestrel. Веб-сервер не нуждается в установке и входит в состав <a href="https://github.com/MultifactorLab/multifactor-selfservice-portal/releases" target="_blank">сборки</a>. Типовая схема работы веб-приложений на стеке .NET 6 для Linux следующая:
1. Приложение запускается на веб-сервере Kestrel. Веб-сервер слушает запросы на определенный порт (пусть 5000) локального хоста по схеме `http`.
2. Реверс-прокси, настроенный на Linux-сервере, слушает запросы на внешний порт сервера по схеме `https` и перенаправляет их на локальный порт 5000.
3. Веб-сервер Kestrel обрабатывает входящие запросы и отправляет их на обработку в приложение.

Таким образом приложение портала располагается за реверс-прокси и обрабатывает запросы только от него.  

### 1. Настройка среды
Для работы портала необходимы пакеты .NET 6 runtime.  
> Дополнительную информацию можно найти <a href="https://docs.microsoft.com/ru-ru/dotnet/core/install/linux#microsoft-packages" target="_blank">здесь</a>. 

Добавьте ключ подписывания пакета в список доверенных ключей, затем добавьте репозиторий пакетов:
```
wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
``` 
Установите среду:
```
sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-6.0
```  

Создайте директории:
```
sudo mkdir /opt/multifactor /opt/multifactor/sspl /opt/multifactor/sspl/app
sudo mkdir /opt/multifactor/sspl/logs /opt/multifactor/sspl/key-storage
```
Создайте пользователя и настройте права:
```
sudo useradd mfa

sudo chown -R mfa: /opt/multifactor/sspl
sudo chmod -R 700 /opt/multifactor/sspl
```

### 3. Копирование файлов
Скачайте и распакуйте файлы приложения:
```
sudo wget https://github.com/MultifactorLab/multifactor-selfservice-portal/releases/latest/download/MultiFactor.SelfService.Linux.Portal.zip

sudo unzip MultiFactor.SelfService.Linux.Portal.zip -d $app_dir
```

### 4. Настройка Nginx
```
sudo apt-get install nginx
sudo service nginx start
```
Перейдите в браузере по адресу `http://<server_IP_address>/index.nginx-debian.html` и убедитесь, что отображается стандартная страница Nginx.

Настройте nginx в режиме реверс-прокси. Откройте файл:
```
sudo vi /etc/nginx/sites-available/default
```
Замените содержимое:
```
server {
  # dns сервера с порталом
  server_name sspl.domain.org;

  location / {
    # http://хост:порт Kestrel
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

Для проверки конфигурации выполните:
```
sudo nginx -t
```
Если конфигурация верна, примените ее:
```
sudo nginx -s reload
```

По умолчанию прокси-сервер работает с незащищенным http-соединением. Если требуется установить сертификат и настроить https, файл `/etc/nginx/sites-available/default` должен выглядеть следующим образом:
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

  # слушать порт 443
  listen 443 ssl;
  # настройки ssl
  ssl_certificate /etc/letsencrypt/live/sspl.multifactor.dev/fullchain.pem;
  ssl_certificate_key /etc/letsencrypt/live/sspl.multifactor.dev/privkey.pem;
  include /etc/letsencrypt/options-ssl-nginx.conf;
  ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;

}

# резервный сервер для перенаправления запросов http на https
server {
  if ($host = sspl.domain.org) {
      return 301 https://$host$request_uri;
  }

  listen 80;

  server_name domain.org;
  return 404;
}
```
Здесь для получения сертификата использовались сервис <a href="https://letsencrypt.org/" target="_blank">Let's Encrypt</a> и утилита `certbot`. Похожие настройки можно получить, следуя <a href="https://www.nginx.com/blog/using-free-ssltls-certificates-from-lets-encrypt-with-nginx/" target="_blank">инструкции</a>.


### 5. Создание службы systemd
Создайте файл службы:
```
sudo vi /etc/systemd/system/sspl.service
```
```
[Unit]
Description=Self Service Portal for Linux

[Service]
WorkingDirectory=/opt/multifactor/sspl/app
ExecStart=/usr/bin/dotnet /opt/multifactor/sspl/app/MultiFactor.SelfService.Linux.Portal.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
TimeoutStopSec=90
SyslogIdentifier=sspl-service
User=mfa
Environment=ASPNETCORE_ENVIRONMENT=production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

СОхраните файл и включите службу:
```
sudo systemctl enable sspl.service
```
Затем запустите ее и убедитесь, что служба работает:
```
sudo systemctl start sspl.service
sudo systemctl status sspl.service
```
В дальнейшем после каждого изменения настроек службы нужно перезапускать подсистему служб и сервис:
```
sudo systemctl daemon-reload
sudo systemctl restart sspl.service
```

## Журналы

Журналы работы портала записываются в `syslog` и сохраняются в текстовые файлы в директорию `/opt/multifactor/sspl/logs`. Если директория пуста или ее нет, нужно убедиться, что у пользователя, под которым запускается служба, достаточно прав.  

Для просмотра содержимого syslog можно воспользоваться командой: 
```
less /var/log/syslog
```

Для просмотра журналов службы sspl.service используйте команду:
```
sudo journalctl -fu sspl.service
```

## Доступ к порталу

Портал доступен по адресу `https://sspl.domain.org`.

Для реализации liveness check используйте GET https://sspl.domain.org/api/ping.  
Пример ответа:
```json
{
    "timeStamp": "2022-08-05T08:19:42.336Z",
    "message": "Ok"
}
```

## Сценарии использования

Портал используется для самостоятельной регистрации второго фактора аутентификации пользователями внутри корпоративной сети, а также выполняет роль единой точки входа для приложений, работающих по технологии Single Sign-On.

После настройки второго фактора, пользователи могут использовать его для безопасного подключения удаленного доступа через VPN, VDI или Remote Desktop.

Смотрите также:
- Двухфакторная аутентификация [Windows VPN со службой Routing and Remote Access Service (RRAS)](https://multifactor.ru/docs/windows-2fa-rras-vpn/)
- Двухфакторная аутентификация [Microsoft Remote Desktop Gateway](https://multifactor.ru/docs/windows-2fa-remote-desktop-gateway/)

## Лицензия

Портал распространяется бесплатно по лицензии [MIT](LICENSE.md).