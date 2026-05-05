import os
import sys
import discord

TOKEN = os.getenv("DISCORD_TOKEN")
CHANNEL_ID = os.getenv("CHANNEL_ID")

VERSION = os.getenv("VERSION")
NAME = os.getenv("RELEASE_NAME") or "Update"
CHANGELOG = os.getenv("CHANGELOG") or "No changelog provided."
CHANGELOG = CHANGELOG.replace("\\n", "\n")
REPO = os.getenv("GITHUB_REPOSITORY")  # e.g. kkorenn/KorenResourcePack

if not REPO:
    print("[ERROR] Missing GITHUB_REPOSITORY")
    sys.exit(1)

# Auto-generated URLs
DOWNLOAD_URL = f"https://github.com/{REPO}/releases/download/v{VERSION}/KorenResourcePack.zip"
RELEASE_URL = f"https://github.com/{REPO}/releases/tag/v{VERSION}"

# Validation
missing = []
for key, value in {
    "DISCORD_TOKEN": TOKEN,
    "CHANNEL_ID": CHANNEL_ID,
    "VERSION": VERSION,
    "GITHUB_REPOSITORY": REPO,
}.items():
    if not value:
        missing.append(key)

if missing:
    print(f"[ERROR] Missing env vars: {', '.join(missing)}")
    sys.exit(1)

CHANNEL_ID = int(CHANNEL_ID)

intents = discord.Intents.default()
client = discord.Client(intents=intents)


class ReleaseView(discord.ui.View):
    def __init__(self):
        super().__init__(timeout=None)

        # Download button
        self.add_item(
            discord.ui.Button(
                label="⬇ Download",
                style=discord.ButtonStyle.link,
                url=DOWNLOAD_URL
            )
        )

        # GitHub button
        self.add_item(
            discord.ui.Button(
                label="🔗 View on GitHub",
                style=discord.ButtonStyle.link,
                url=RELEASE_URL
            )
        )


@client.event
async def on_ready():
    print(f"[INFO] Logged in as {client.user}")

    channel = client.get_channel(CHANNEL_ID)
    if channel is None:
        try:
            channel = await client.fetch_channel(CHANNEL_ID)
        except discord.DiscordException as ex:
            print(f"[ERROR] Could not find channel: {ex}")
            await client.close()
            return

    embed = discord.Embed(
        title="🚀 New Update!",
        description=f"**v{VERSION} {NAME}**",
        color=0x5865F2
    )

    embed.add_field(
        name="📜 Changelog",
        value=CHANGELOG,
        inline=False
    )
    embed.set_footer(text="Koren > Jipper")

    await channel.send(content="<@&1501202364302889142>",embed=embed, view=ReleaseView())

    print("[INFO] Message sent, shutting down.")
    await client.close()


client.run(TOKEN)
