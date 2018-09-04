#!/bin/bash

for file in ./*.vert
do
echo "Compiling vertex shader $file..."
/opt/VulkanSDK/1.0.65.0/x86_64/bin/glslangValidator -V $file -o "$file.spv"
done

for file in ./*.frag
do
echo "Compiling fragment shader $file..."
/opt/VulkanSDK/1.0.65.0/x86_64/bin/glslangValidator -V $file -o "$file.spv"
done
