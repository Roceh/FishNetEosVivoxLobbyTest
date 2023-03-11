# Basic Unity project for testing FishNet, Epic Online Services Relay & Lobbies & Vivox voice comms (lobby and positional)

Epic very kindly offer a free relay service (EOS), which you do not need to login to Epic or any other online service to use. It can be used with an anonymous device login, so if you want to release a game with multiplayer on Itch for example you can use EOS and force relays and get seemless AWS server relaying for free! 

EOS does have voice comms, but no positional, however Unity offer Vivox voice comms for free up to 5000 concurrent users, which does allow positional voice comms. I've used the Vivox offered by Unity (you can get it direct from Vivox site instead) as there is no approval process for that one and you can use Unitys authentication process for Vivox login.

Project was done in Unity 2021.3.20f1

Some notes:

- For EOS relay you will need to setup an EOS account - follow the instructions on the EOS Plugin for Unity github page. (Ultimately you will need to fill in the info in Tools->EpicOnlineServicesConfigEditor)
- For Vivox you will need to link your project to a Unity vivox Id - see https://support.unity.com/hc/en-us/articles/6380084154772-Vivox-How-do-I-get-started-with-Vivox-in-my-Unity-Project-
- It might be worth trying EOS/Vivox samples first to be sure you have got the process right.  
- The lobby join is sometimes a bit glitchy, not sure where the problem lies - to be fixed :)
- You can't run up two instances of the app on the same PC and relay via EOS and use the same anonymous login for both. They need to be different PCs as the anonymous device login can only be used once. 
- I will look at adding a better setup guide soon.
- I will improve the lobby and also add a bit more to the actual 'game' part.

---
#### Free assets used

FishNet: https://assetstore.unity.com/packages/tools/network/fish-net-networking-evolved-207815

FishyEOS: https://github.com/ETdoFresh/FishyEOS

EOS Plugin for Unity: https://github.com/PlayEveryWare/eos_plugin_for_unity

Free Tank Model: https://assetstore.unity.com/packages/3d/vehicles/land/cartoon-tank-free-165189

Simple Button Set 01: https://assetstore.unity.com/packages/2d/gui/icons/simple-button-set-01-153979

---
MIT License

Copyright (c) 2023 David Dunscombe

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
