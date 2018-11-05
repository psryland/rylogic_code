## Windows Subsystem for Linux

The linux version of the Wallet can be build, executed, and debugged using the Windows Subsystem for Linux.
Install WSL by executing the following in an admin powershell instance:

	Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux

Then, install Ubuntu 16.04 from the Microsoft store. (Restart needed)
This will give you a clean Ubuntu instance. From any command line you should now be able to type 'bash' and be dropped into a bash shell at the current directory location.

Use the 'apt' tool to install the packages and project dependencies

### Installing dotnet on WSL

Add the sources list:

	wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
	sudo mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
	wget -q https://packages.microsoft.com/config/ubuntu/16.04/prod.list 
	sudo mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
	sudo chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
	sudo chown root:root /etc/apt/sources.list.d/microsoft-prod.list

Install the dotnet 2.1 sdk:

	sudo apt install apt-transport-https
	sudo apt update
	sudo apt install dotnet-sdk-2.1

### Tools and Dependencies

Ensure you have these tools:

	- Git - Needed to pull the thirdparty library source code.
		- On Linux, this should already be installed
		- On windows, this is installed with visual studio.

	- CMake - Needed to build Electroneum, Monero
		- On Linux, ```sudo apt install cmake```
		- On Windows, use https://cmake.org/files/v3.11/cmake-3.11.3-win64-x64.msi

	- Python3 - Needed for the main project setup (Setup.py)
		- On Linux, should already be installed
		- On windows, this is installed with visual studio.

	- boost1.58.0-dev
		- On Linux, ```sudo apt install boost1.58-all-dev```
		- On Windows, download https://sourceforge.net/projects/boost/files/boost/1.66.0/boost_1_66_0.zip (Using version 1.66 because VS2017 is not supported for version 1.58)

	- OpenSSL
		- On Linux, ```sudo apt install libssl-dev```
		- On Windows, git clone https://github.com/openssl/openssl.git

	- libunbound-dev
		- On Linux, ```sudo apt install libunbound-dev```

	- Pkg-Config - Needed for Linux builds
		- On Linux, ```sudo apt install pkg-config```

	- Perl - Needed to build OpenSSL
		- On windows, use https://www.activestate.com/activeperl/downloads 

There is a helper script that can be used to download the required packages and install them in the expected locations. Run 'Setup.py' in the script directory to get, update, and build the dependencies.

The windows version of the Wallet requires the third party libraries (boost, openssl, etc) to be available in the 'sdk' directory in the root level of the project.

### Installing the ODBC driver for SQL Server on WSL

Download the package list from microsoft:

	sudo su
	curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add -
	curl https://packages.microsoft.com/config/ubuntu/16.04/prod.list > /etc/apt/sources.list.d/mssql-release.list
	exit

Update and install msodbcsql17:

	sudo apt-get update
	sudo ACCEPT_EULA=Y apt install msodbcsql17

Optional, for bcp and sqlcmd:

	sudo ACCEPT_EULA=Y apt install mssql-tools
	echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bash_profile
	echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc
	source ~/.bashrc

The wallet application needs to connect to SQL Server. To do this, the connection from WSL needs to use an explicit user login rather than the windows local user login. Run an admin instance of MS SQL Management Studio and set up a new login (Security -> Logins -> New Login).

	- Create a user name
	- Select SQL Server authentication
	- Set a password
	- Uncheck 'Enforce password policy'
	- Go to 'User Mapping'
	- Give access to the wallet database(s).
	- Check 'db_datawriter' and 'db_datareader' for each DB the user has access to

 Add an entry to "ConnectionStrings" in the 'appsettings.json' file with this form:
 ```<connection_name>": "Server=localhost; Database=ETN; Trusted_Connection=False; MultipleActiveResultSets=true; User Id=<user>; Password=<password>"```

Connection from WSL to SQL Server running on Windows requires the TCP/IP protocol enabled. Run the 'Sql Server Configuration Manager' select 'SQL Server Network Configuration' -> 'Protocols for MSSQLSERVER' -> Enable TCP/IP. You can test the connection in WSL using the command: ```nc -zv localhost 1433``` (SQL uses port 1433). If you get "Connection to localhost 1433 port [tcp/ms-sql-s] succeeded!" then you should be good to connect. Test the sql connection using 'sqlcmd': ```sqlcmd -S localhost -d <db_name> -U <user> -P <password>```

Also, Remember to allow 'SQL Server Authentication'. In MS SQL Server Management Studio; SQL Server -> Properties -> Security; Set 'Server authentication' to 'SQL Server and Windows Authentication mode'. You will need to restart SQL Server (done via context menu on SQL Server)

### Remote Debugging WSL

Ensure the build-essential package is installed in WSL using:

	sudo apt install -y build-essential

Also, ensure zip and unzip packages are installed:

	sudo apt install zip unzip

You also need to install 'gdbserver', a program that allows you to debug with a remote GDB debugger:

	sudo apt install -y gdbserver

Next install ssh server:

	sudo apt install -y openssh-server

Then edit the file '/etc/ssh/sshd_config' to ensure the "PasswordAuthentication" setting is set to 'yes'. Do this from within WSL, editing linux files from windows corrupts the linux filesystem:

	sudo nano /etc/ssh/sshd_config

Now generate SSH keys for the SSH instance:

	sudo ssh-keygen -A

Start SSH before connecting from Visual Studio. You will need to do this every time you start your first Bash console. As a precaution, WSL currently tears-down all Linux processes when you close your last Bash console!.

	sudo service ssh start

Load the 'Visual Studio Installer' application and make sure 'Linux development with C++' is installed.

Now you can connect to WSL from Visual Studio by going to Tools > Options > Cross Platform > Connection Manager. Click add and enter “localhost” for the hostname and your WSL user/password.

Launch the application in a WSL bash shell, then attach the VS debugger with Connection Type = 'SSH' and Connection target = 'user@localhost'
