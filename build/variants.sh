#!/usr/bin/env bash
# Runs the tests again on Linux as Windows and macOS meet a script, so a fault of those systems
# fails here before a workflow finds it. The temporary folder is reached through a symbolic link,
# as macOS reaches /var by /private/var, so a path given through a link and one git gives must be
# read alike, every program the tests start has a single-byte locale, as Windows gives a pipe
# cp1252, so a script that prints what it cannot encode fails, and git turns line ends to CRLF as
# Git for Windows does by its own setting, so a test whose git calls differ in it fails. The
# tests must be built first.
#
#   build/variants.sh [arguments for dotnet test]
set -euo pipefail
cd "$(dirname "$0")/.."

locale=en_US.ISO-8859-1
if ! LC_ALL="$locale" python3 -I -c 'import sys; sys.exit(sys.stdout.encoding != "iso8859-1")' 2>/dev/null; then
  echo "The locale $locale is not installed here, so no single-byte run can be made. sudo locale-gen en_US installs it on Ubuntu, and sudo locale-gen $locale on Fedora." >&2
  exit 2
fi

real="$(mktemp -d)"
link="$real-linked"
ln -s "$real" "$link"
trap 'rm -rf "$real"; rm -f "$link"' EXIT
printf '[core]\n\tautocrlf = true\n' > "$real/gitconfig"

GIT_CONFIG_GLOBAL="$real/gitconfig" TMPDIR="$link" LC_ALL="$locale" TOOLS_VARIANT=1 dotnet test Tools.Tests --no-build "$@"
