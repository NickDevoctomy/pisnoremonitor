# pisnoremonitor

Snoring monitor created for Raspberry Pi.

This is a very early prototype. What I plan on implementing is the following,

1. .net 8 Avalonia app to run in Kiosk mode on the PI
2. Ability to record nights of audio and input from other sensors (TBD)
3. Ability to inject tags into the recording during a session to mark that something happened, for personal reference primarily.
4. Snoring analysis of the session

In order to perform real-time snoring analysis I want to build a model using ML. I plan on running the model on the device to allow it to analyse the session in real-time. No idea how this is going to work out as I haven't done any ML before! So bear with me.

**Ultimate Goal**

What I'm trying to achieve is a self contained unit for monitoring your snoring with the intention of improving your quality of life / sleep. Also to remove the requirement for needing a subscription based service.

## Recommended Hardware

* Raspberry PI 4B
* [Elecrow Touch Screen](https://www.amazon.co.uk/dp/B081QFJHG7?ref=ppx_yo2ov_dt_b_fed_asin_title)
  - I have this configured to put itself to sleep after 60 seconds using swayidle (wtf that is).
  - I tried an [Aurevita for Raspberry Pi 3.5" Screen](https://www.amazon.co.uk/dp/B0DQ858YQV?ref=ppx_yo2ov_dt_b_fed_asin_title&th=1) - But it is not suitable as the screen cannot be turned off. It   
* [Mini USB Micrphone](https://www.amazon.co.uk/dp/B0DH1TY54Y?ref=ppx_yo2ov_dt_b_fed_asin_title)
* (Recommended Microphone) [Keyestudio 5V ReSpeaker 2 Mic Pi HAT 1.0](https://www.aliexpress.com/item/32902300949.html?spm=a2g0o.order_list.order_list_main.5.2e8d1802doU5Ab) - I tried this out after finding the mini USB one above being too quiet. I had to install it with help from ChatGpt, but it didn't take too long and this one is *much* better.
also introduces more heat than is desirable.
* USB Storage Device (You'll want something quite sizeable so that you can record without worrying about space.)
  - Ideally you should use a short USB extension lead so that the storage device isn't directly inside the Pi. They get extremely hot and heat up everything around them.

## Installation Instructions

### Requirements

We will start by assuming you are booting into a vanilla installation of Raspberry PI OS without dotnet 8.

#### Install dotnet 8

1. Install dotnet 8

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version latest --verbose
```

2. Update environment variables

```bash
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc
```

#### Clone, Build and Run from Source

Open a new terminal and run the following,

```bash
mkdir ~/src
cd ~/src
git clone https://github.com/NickDevoctomy/pisnoremonitor
cd pisnoremonitor
dotnet restore
dotnet build
cd PiSnoreMonitor
dotnet run
```

> Please note, the application will not be able to record any audio until libportaudio2 is installed, as per the instructions below. This is caused by PortAudioSharp2 not including ARM64 binaries. This will be fixed in due coarse.

#### Install libportaudio2

Open a new terminal and run the following,

```bash
sudo apt update
sudo apt install portaudio19-dev
~/src/pisnoremonitor/PiSnoreMonitor/bin/Debug/net8.0/runtimes/linux-arm64/native
cp /usr/lib/aarch64-linux-gnu/libportaudio.so .
```

> The application should now run and be able to record.

#### Simple Desktop Icon to test with

Open a new terminal and run the following,

```bash
cd ~/Desktop/
nano PiSnoreMonitor.desktop  
```

paste the following, and then use Ctrl+O followed by return, then Ctrl+X.

```
[Desktop Entry]
Version=1.0
Type=Application
Name=PiSnoreMonitor
Comment=Run prototype of PiSnoreMonitor
Exec=bash -lc 'PATH="$HOME/.dotnet:$PATH"; cd ~/src/pisnoremonitor/PiSnoreMonitor; dotnet run; echo; read -n1 -s -r'
Terminal=true
Categories=Development;
```

An icon should now appear on the desktop, open it and choose to execute in the terminal.

## Additional Information

#### Sway Screeen Blanking

The following should blank the screen after 60 seconds of inactivity.

```bash
mkdir -p ~/.config/labwc
cat > ~/.config/labwc/autostart <<'EOF'
/usr/bin/swayidle -w -C /dev/null \
  timeout 60 'wlopm --off "*"' \
  resume  'wlopm --on "*"' &
EOF
```