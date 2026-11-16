# Search without opening a browser and typing the address first.
# Run from Scripts > SearchTheWeb, or bind your own shortcut.

query = dialog.ask("Search the web for:", clipboard.gettext())

if text.trim(query) != ""
    shell.open("https://www.google.com/search?q=" + text.urlencode(query))
end
