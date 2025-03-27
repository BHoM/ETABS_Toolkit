
[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg)](https://www.gnu.org/licenses/lgpl-3.0) [![Build status](https://ci.appveyor.com/api/projects/status/pc3au0y87vcl05tf/branch/master?svg=true)](https://ci.appveyor.com/api/projects/status/etabs_toolkit/branch/master) [![Build Status](https://dev.azure.com/BHoMBot/BHoM/_apis/build/status/ETABS_Toolkit/ETABS_Toolkit.CheckCore?branchName=master)](https://dev.azure.com/BHoMBot/BHoM/_build/latest?definitionId=81&branchName=master)

# ETABS_Toolkit

This toolkit allows interoperability between the BHoM and CSI ETABS. Enables creation, manipulation and reading of structural finite element analysis models as well as loading information and extraction of analysis results.

https://www.csiamerica.com/products/etabs

### Known Versions of Software Supported

#### Built and tested:
CSI ETABS 2016
CSI ETABS 17
CSI ETABS 18
CSI ETABS 20
CSI ETABS 21

#### Not supported:
CSI ETABS 22 due to internal failures of the ETABS API. A fix for this is being worked on.

### Net runtime issues

There are currently some internal failures in the ETABS API when called in a NET Core environment. For this reason, running the ETABSAdapter in runtimes above net 4 is disabled.

If you are using the ETABS Adapter with Grasshopper in Rhino 8 you can change the runtime used by Rhino to framework. To do this, please see this link: https://www.rhino3d.com/en/docs/guides/netcore/#to-change-rhino-to-always-use-net-framework

A fix to allow for higher net runtimes is being worked on.


### Documentation
For more information about functionality, currently supported types and known issues see [ETABS_Toolkit wiki](https://github.com/BHoM/ETABS_Toolkit/wiki)

---
This toolkit is part of the Buildings and Habitats object Model. Find out more on our [wiki](https://github.com/BHoM/documentation/wiki) or at [https://bhom.xyz](https://bhom.xyz/)

## Quick Start 🚀 

Grab the [latest installer](https://bhom.xyz/) and a selection of [sample scripts](https://github.com/BHoM/samples).


## Getting Started for Developers 🤖 

If you want to build the BHoM and the Toolkits from source, it's hopefully easy! 😄 
Do take a look at our specific wiki pages here: [Getting Started for Developers](https://bhom.xyz/documentation/Guides-and-Tutorials/Coding-with-BHoM/)

In order to support multiple versions of ETABS with changes to the API, multiple build configurations have been set up. These all rename the resulting dll of the adapter project in order to support multiple versions to be installed simultaneously. ETABS_Toolkit needs to be built separately for each version of ETABS. To switch between version and specific ETABS_Toolkit configurations use Configuration Manager:  
Debug16 -> ETABS 2016   
Debug17 -> ETABS 17  
Debug18 -> ETABS 18 

## Want to Contribute? ##

BHoM is an open-source project and would be nothing without its community. Take a look at our contributing guidelines and tips [here](https://github.com/BHoM/BHoM/blob/main/CONTRIBUTING.md).


## Licence ##

BHoM is free software licenced under GNU Lesser General Public Licence - [https://www.gnu.org/licenses/lgpl-3.0.html](https://www.gnu.org/licenses/lgpl-3.0.html)  
Each contributor holds copyright over their respective contributions.
The project versioning (Git) records all such contribution source information.
See [LICENSE](https://github.com/BHoM/BHoM/blob/main/LICENSE) and [COPYRIGHT_HEADER](https://github.com/BHoM/BHoM/blob/main/COPYRIGHT_HEADER.txt).

