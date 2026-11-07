# Componentes de terceiros

Distribuição UGA 1.0.0 para Windows x64, sem drivers incluídos.

- SDL3-CS 3.4.20.0 e SDL3-CS.Windows 3.4.20.0: https://github.com/edwardgushchin/SDL3-CS — licença zlib. O pacote nativo contém SDL3 3.4.20, https://github.com/libsdl-org/SDL — licença zlib.
- Nefarius.ViGEm.Client 1.21.256: https://github.com/nefarius/ViGEm.NET — licença MIT. O cliente nativo ViGEmClient é incorporado ao assembly pelo próprio pacote (recursos costura32/costura64) e extraído/carregado pelo pacote. A publicação não precisa de um arquivo ViGEmClient.dll externo fornecido pelo UGA.
- Runtime .NET 10 / Windows Desktop: https://github.com/dotnet — licenças e avisos distribuídos nos arquivos LICENSE.txt e ThirdPartyNotices.txt do runtime publicado.
- xUnit e Microsoft.NET.Test.Sdk são dependências somente de desenvolvimento, não da publicação do aplicativo.

## Licença zlib

Copyright (c) 2024-2026 Eduard Gushchin (SDL3-CS).
SDL: copyright dos respectivos autores, incluindo Sam Lantinga.

This software is provided 'as-is', without any express or implied warranty. In no event will the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose, including commercial applications, and to alter it and redistribute it freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.

## Licença MIT — ViGEm.NET / ViGEmClient

Copyright (c) 2018 Benjamin Höglinger-Stelzer.
Pacote gerenciado: Copyright © Nefarius Software Solutions e.U. 2017-2023.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
