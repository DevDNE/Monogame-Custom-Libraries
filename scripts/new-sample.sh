#!/usr/bin/env bash
# Scaffold a new sample game project under src/. Copies the template/
# directory, renames every __SAMPLE__ marker to the user-provided name,
# and adds the project to Game.sln.
#
# Usage:   scripts/new-sample.sh <Name>
# Example: scripts/new-sample.sh ActionRPG
#          → creates src/MonoGame.GameFramework.ActionRPG/ and wires it
#            into the solution.

set -euo pipefail
cd "$(dirname "$0")/.."

NAME="${1:-}"
if [ -z "$NAME" ]; then
  echo "usage: $0 <Name>"
  echo "example: $0 ActionRPG"
  exit 2
fi

# Validate: PascalCase-ish, no dots/slashes/whitespace.
if ! [[ "$NAME" =~ ^[A-Z][A-Za-z0-9]*$ ]]; then
  echo "Name must start with uppercase letter and contain only letters/digits."
  echo "Got: '$NAME'"
  exit 2
fi

PROJ_DIR="src/MonoGame.GameFramework.$NAME"
CSPROJ="$PROJ_DIR/MonoGame.GameFramework.$NAME.csproj"

if [ -d "$PROJ_DIR" ]; then
  echo "Refusing to overwrite existing project: $PROJ_DIR"
  exit 1
fi
if [ ! -d "template" ]; then
  echo "Missing template/ directory — run from repo root."
  exit 1
fi

echo "Creating $PROJ_DIR ..."
mkdir -p "$PROJ_DIR"
cp -R template/. "$PROJ_DIR/"

# Rename the placeholder .csproj
mv "$PROJ_DIR/MonoGame.GameFramework.__SAMPLE__.csproj" "$CSPROJ"

# Replace __SAMPLE__ marker throughout all files.
# -i '' for macOS sed, omit '' for GNU sed; detect which we have.
sed_inplace() {
  if sed --version >/dev/null 2>&1; then
    sed -i "$@"
  else
    sed -i '' "$@"
  fi
}
# Only text files. The template ships binary content (Content/sprites/*.png),
# and macOS sed aborts with "RE error: illegal byte sequence" the moment it is
# handed a file containing non-UTF-8 bytes — which, with set -e, kills the
# scaffold half-created. LC_ALL=C additionally stops sed interpreting bytes as
# multibyte characters in filenames or content.
while IFS= read -r -d '' file; do
  LC_ALL=C sed_inplace "s/__SAMPLE__/$NAME/g" "$file"
done < <(find "$PROJ_DIR" -type f \
  \( -name '*.cs' -o -name '*.csproj' -o -name '*.mgcb' \
     -o -name '*.spritefont' -o -name '*.json' -o -name '*.md' \
     -o -name '*.pix' -o -name '*.gpl' \) -print0)

echo "Adding to Game.sln ..."
dotnet sln Game.sln add "$CSPROJ"

echo
echo "Building solution ..."
if ! dotnet build Game.sln -c Debug --nologo -v minimal >/dev/null; then
  echo "WARNING: build failed. Inspect $PROJ_DIR and try again."
  exit 1
fi

echo
echo "Done. Run with:"
echo "  dotnet run --project $CSPROJ"
echo
echo "Smoke-test all samples (including the new one):"
echo "  scripts/smoke-all.sh"
