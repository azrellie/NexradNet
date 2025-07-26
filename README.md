For sources of Nexrad level 3 data, refer to these urls:
1. ftp://tgftp.nws.noaa.gov/ (FTP server, go to **/SL.us008001/DF.of/DC.radar/** for the data. Can be used in Windows file explorer)
2. https://registry.opendata.aws/noaa-nexrad/
3. https://console.cloud.google.com/storage/browser/gcp-public-data-nexrad-l3;tab=objects?pli=1&invt=AbulJA&prefix=&forceOnObjectsSortingFiltering=false
4. https://console.cloud.google.com/storage/browser/gcp-public-data-nexrad-l3-realtime;tab=objects?inv=1&invt=Ab30-A&prefix=&forceOnObjectsSortingFiltering=false&pageState=(%22StorageObjectListTable%22:(%22f%22:%22%255B%255D%22)) (for real time level 3 from Google)

# Nexrad.NET
---
Nexrad.NET is a C# library created for the purpose of reading and processing NEXRAD Level 3 radar files.

# Overview
---

### Basic Usage
This library is pretty straightfoward in its usage.
Here is a basic example of decoding a radar file:
```cs
BinaryReader reader = new(File.OpenRead("super_res_base_ref_file"));
Level3 level3 = new(ref reader);
```

### Arguments
The Level3 object has different arguments. They are specified as:
```cs
Level3 level3 = new(
    ref reader, // binary reader, which is always required and is the only required arg. (must be passed as reference via ref keyword)
    processingStages = 0b111, // processing stages. see below for more details.
    textHeaderLength = -1, // the length of the text header, if known. default value is -1.
    guessFormat = true, // tells the object to try to guess the format and text header length. default value is true.
    enableDebuggingLogging = true // tells the object to output to console for logging reasons.
);
```

### Unsupported Products
Currently, there are 4 unsupported products that may get support in the future.
Those products are:
1. Storm Structure (code=62)
2. Melting Layer (code=166)
3. Digital Precipitation Array (code=81)
4. Supplemental Precipitation Data (code=82)

### Processing Stages
The Level3 object has a processing stages argument which tells the class what parts of the product to decode. The value is structured as "0b111". This value is composed of 3 bits, each one telling the program what part to decode.

Basic overview of the bit stucture:
```
0b111
  ^^^
  |||
  ||⤷ Bit 3. Decode symbology block if set to 1.
  |⤷ Bit 2. Decode graphic block if set to 1.
  ⤷ Bit 1. Decode tabular block if set to 1.

Setting any of the bits to 0 will make that block not get processed. Can be used to save memory and processing time if you do not need the other blocks.
```
