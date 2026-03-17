#!/bin/bash
# Aspire startup delay for macOS/Linux
if [ "$1" == "" ]; then
  echo "Usage: $0 <seconds>"
  exit 1
fi
sleep "$1"
echo "Delay finished."
