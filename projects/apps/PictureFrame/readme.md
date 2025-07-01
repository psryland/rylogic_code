# Picture Frame Setup

## Raspberry Pi

1. Make sure raspi-config->Advanced->Wayland is set to x11

    ```sh
    sudo raspi-config
    # select Advanced -> Wayland => Set to X11
    ```

1. Clone the Rylogic repo to somewhere on the Pi (e.g. `~/rylogic_code`)

    ```sh
    git clone https://github.com/psryland/rylogic_code.git
    ```

1. Install these required python libraries

    ```sh
    sudo apt update
    sudo apt install mpv
    sudo apt install python3-mpv
    sudo apt install python3-pathspec
    ```

1. Configure MPV with this configuration:

    ```sh
    # ~/.config/mpv/mpv.conf
    vo=gpu
    hwdec=v4l2m2m
    x11-bypass-compositor=yes
    ```

1. Mount the ZDrive into `/mnt/ZDrive`
    1. Add support for smb

        ```sh
        sudo apt install cifs-utils
        ```

    1. Create the mount point

        ```sh
        sudo mkdir -p /mnt/ZDrive
        ```

    1. Edit the `/etc/fstab' file

        ```pgsql
        //<server-ip-or-name>/<share-name> /mnt/myshare cifs _netdev,credentials=/home/pi/.smbcredentials,iocharset=utf8,vers=3.0 0 0
        ```

    1. Create a credentials file

        ```sh
        nano ~/.smbcredentials
        ```

        Containing:

        ```ini
        username=your_smb_username
        password=your_smb_password
        ```

        Then protect with:

        ```sh
        chmod 600 ~/.smbcredentials
        ```

        Test with:

        ```sh
        sudo mount -a
        ```

1. Build the image list

    ```py
    python picture.py --scan
    ```

1. Run

    ```py
    python picture.py
    ```

1. Create a Desktop shortcut

  1. Create a `PictureFrame.desktop` file on the desktop

  ```bash
  nano ~/desktop/PictureFrame.desktop
  ```

  1. Add this to the file

  ```ini
  [Desktop Entry]
  Name=Picture Frame
  Comment=Launch Picture Frame
  Exec=python3 /home/pi/rylogic_code/projects/apps/PictureFrame/picture.py
  Icon=/home/pi/rylogic_code/projects/apps/PictureFrame/icon.png
  Terminal=false
  Type=Application
  ```

  1. Make it executable

  ```bash
  chmod +x ~/Desktop/PictureFrame.desktop
  ```