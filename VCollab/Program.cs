// See https://aka.ms/new-console-template for more information

using osu.Framework;
using VCollab;

using var host = Host.GetSuitableDesktopHost("VCollab");
using var game = new VCollabGame();

host.Run(game);