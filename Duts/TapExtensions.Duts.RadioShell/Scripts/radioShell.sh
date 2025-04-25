#!/bin/bash

echo "Welcome to RadioShell"
echo -n '$ '

while read -r cmd; do

  if [ -n "$cmd" ]; then
    echo -e -n "ret $cmd "

    case $cmd in
      hello)
        echo "==> done ok"
        ;;
      123)
        echo -n "==> done ok "
        echo "456"
        ;;
      d)
        echo -n "==> done ok "
        date
        ;;
      f)
        echo "==> done failed"
        ;;
      q|quit|bye)
        break
        ;;
      *)
        echo "==> error 0x05"
        ;;
    esac

  fi

  echo -n '$ '

done

