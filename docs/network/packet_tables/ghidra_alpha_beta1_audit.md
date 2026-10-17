Alpha/Beta1 Ghidra Packet Audit
==============================================================================

Scope
------------------------------------------------------------------------------

- Alpha: `/RagExe-Alpha.exe`, `RagExe.exe`, 2001-08-30 iRO Alpha.
- Beta1: `/Ragexe-Beta.exe`, `Ragexe.exe`, 2002-02-20 iRO Beta1.
- Ghidra image base for both programs: `0x00400000`.

Labels Applied
------------------------------------------------------------------------------

Alpha:

- `Alpha_Net_InitPacketLengthTable` at `00415820`
- `Alpha_PacketTable_HasFixedSize` at `00417630`
- `Alpha_PacketTable_GetSize` at `00417750`
- `Alpha_Net_TryReadPacket` at `004179b0`
- `Alpha_Net_HasCompletePacket` at `00417a70`
- `Alpha_LoginMode_DispatchPackets` at `0048cbf0`

Beta1:

- `Beta1_Net_InitPacketLengthTable` at `00418040`
- `Beta1_PacketTable_HasFixedSize` at `0041a650`
- `Beta1_PacketTable_GetSize` at `0041a770`
- `Beta1_Net_TryReadPacket` at `0041a9d0`
- `Beta1_Net_HasCompletePacket` at `0041aa90`
- `Beta1_LoginMode_DispatchPackets` at `004bc390`

Confirmed Corrections
------------------------------------------------------------------------------

Alpha's packet-length initializer stores packet lengths in a `std::map`-style
runtime table. The following Alpha rows were corrected in
`PacketTable.0100_Alpha.cs`:

Name                                       | Wire ID  | Client Size
-------------------------------------------|----------|------------
ZC_ACK_REQ_JOIN_GROUP                      | 0x0098   | 19
ZC_REQ_JOIN_GROUP                          | 0x0099   | 22
CZ_REQ_EXPEL_GROUP_MEMBER                  | 0x009C   | 22
ZC_ADD_MEMBER_TO_GROUP                     | 0x009D   | 63
ZC_DELETE_MEMBER_FROM_GROUP                | 0x009E   | 23
CZ_UPGRADE_SKILLLEVEL                      | 0x00AB   | 4

Beta1 keeps the wider 24-character party/member packet bodies. The 190 overlay
therefore restores those sizes after the Alpha base table is loaded, while the
200 overlay keeps `ZC_GROUPINFO_CHANGE` and `CZ_CHANGE_GROUPEXPOPTION` inserted
at `0x009D` and `0x009E`.

Notes
------------------------------------------------------------------------------

- `0xffffffff` in the client initializer corresponds to Sabine's dynamic packet
  size (`PacketTable.Dynamic`).
- The Alpha `0x00AB` row is present in the client packet-length initializer with
  size `4`; after the Beta 190 `0x0027` insertion it shifts to `0x00AC`, matching
  `CZ_UPGRADE_SKILLLEVEL`.
