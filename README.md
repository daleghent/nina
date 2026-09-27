# N.I.N.A. - Nighttime Imaging 'N' Astronomy #
[![Website](https://img.shields.io/badge/website-nighttime--imaging.eu-blue)](https://nighttime-imaging.eu/)
[![Latest Release](https://img.shields.io/badge/download-latest-blue)](https://nighttime-imaging.eu/download/)
[![Discord](https://img.shields.io/discord/436650817295089664)](https://discord.gg/nighttime-imaging)
[![License: MPL 2.0](https://img.shields.io/badge/License-MPL%202.0-brightgreen.svg)](https://www.mozilla.org/en-US/MPL/2.0/)
[![Become a Patron](https://img.shields.io/badge/Patreon-support-orange?logo=patreon)](https://www.patreon.com/stefanberg?fan_landing=true)

This repository contains the source code of the **N.I.N.A. - Nighttime Imaging 'N' Astronomy** imaging software.

---

## 🧭 About

**N.I.N.A.** (Nighttime Imaging 'N' Astronomy) is a modular astrophotography suite designed to simplify and streamline image acquisition.  
Originally created with DSO imaging in mind, the platform now supports a wide range of astrophotography and astronomy workflows, including equipment control, imaging, sequencing, plate solving, and plugin-based extension.

Whether you're new to astrophotography or a seasoned imager, N.I.N.A. aims to make your sessions easier, faster, and more comfortable.

---

## 🗂 Repository

This repository contains the N.I.N.A. application source code, shared libraries, installers, and tests in [`NINA.sln`](NINA.sln). User-facing documentation is maintained separately in [`nina.docs`](https://github.com/isbeorn/nina.docs) and included here as the [`NINA.Docs`](NINA.Docs) submodule.

Choose the reference for your task:

- [AGENTS.md](AGENTS.md) for repository constraints and agent guidance
- [ARCHITECTURE.md](ARCHITECTURE.md) for solution ownership, boundaries and links to all project architecture documents
- [CONTRIBUTING.md](CONTRIBUTING.md) for contributor setup, localization and upstream submission requirements
- [Testing map](.agents/skills/nina-repository/references/testing-map.md) for Windows build commands and test selection

---

## 🛠 Development

Development uses the .NET CLI on Windows. See [developer prerequisites](CONTRIBUTING.md#setting-up-the-developer-environment) and the [local verification commands](.agents/skills/nina-repository/references/testing-map.md#command-setup). The [contribution guide](CONTRIBUTING.md#running-auts-from-the-command-line) distinguishes focused local checks from required CI verification.

---

## 🌐 Resources

- 🏠 Project website: [nighttime-imaging.eu](https://nighttime-imaging.eu/)
- 📦 Latest release: [nighttime-imaging.eu/download](https://nighttime-imaging.eu/download/)
- 📖 Documentation: [nina.docs](https://github.com/isbeorn/nina.docs)
- 💬 Community support: [Discord](https://discord.gg/nighttime-imaging)

---

## 🤝 Contributing

Interested in contributing code, reporting bugs, or improving documentation?  
Please start with the local [Contributing Guidelines](CONTRIBUTING.md).

For documentation-only changes, use the separate [nina.docs](https://github.com/isbeorn/nina.docs) repository and its contribution guide.

We welcome all kinds of contributions — from small fixes to large feature proposals.

---

## ⚖ License

This project is licensed under the **Mozilla Public License 2.0**.  
See the [`LICENSE`](./LICENSE.txt) file for details.
