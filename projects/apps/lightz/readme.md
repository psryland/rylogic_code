# Pauls Moderately Awesome LED Thing

# Cheatsheet

- Open this project in VSCode
- Open terminal
- `clear && idf.py build`
- `clear && idf.py flash`
- `idf.py menuconfig'

## Initial setup

- Use `idf.py set-target esp32s3`. This will recreate the sdkconfig file
- Then run `idf.py menuconfig`
- Serial flasher config ---> Flash size = 16Mb
- Component config ---> ESP System Settings ---> CPU frequency = 240MHz
  (for n16r8)

## Facts

- LED Strip type is 'WS2812B'
- Using 'FastLED' library
- Built-IN RGB led is on pin 47

## Trouble Shooting


