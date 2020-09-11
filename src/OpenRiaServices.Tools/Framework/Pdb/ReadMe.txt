This PDB folder contains a subset of code taken from Mike Stall's PDB-->XML sample.

The latest sample is found at:
    Dev10\pu\clr\qa\clr\testsrc\CoreCLRSDK\devsvcs\tests\tools\pdb2xml
    
This sample required other "core" sample code from:
    Dev10\pu\clr\qa\clr\testsrc\CoreCLRSDK\devsvcs\tests\tools\mdbg\mdbgsource\coreapi\SymStore
    
The following changes were made to the code:
  1) Made FxCop and StyleCop clean
  2) Removed all unnecessary code for PDB writers
  3) Removed ILDB support
  4) Modified the namespace to live in OpenRiaServices.Tools.Pdb and Pdb.SymStore
  5) Made all types internal rather than public
  