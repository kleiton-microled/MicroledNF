# XML Schema Classes

This folder contains C# classes manually created from the XSD files to ensure proper serialization and type safety.

## Implemented Classes

The following classes have been implemented:

### Main Request/Response Classes
- `PedidoEnvioLoteRPS` (from PedidoEnvioLoteRPS_v02.xsd)
- `RetornoEnvioLoteRPS` (from RetornoEnvioLoteRPS_v02.xsd)
- `PedidoConsultaNFe` (from PedidoConsultaNFe_v02.xsd)
- `RetornoConsulta` (from RetornoConsulta_v02.xsd)
- `PedidoCancelamentoNFe` (from PedidoCancelamentoNFe_v02.xsd)
- `RetornoCancelamentoNFe` (from RetornoCancelamentoNFe_v02.xsd)

### Type Classes (from TiposNFe_v02.xsd)
- `tpRPS` - Tipo que representa um RPS
- `tpNFe` - Tipo que representa uma NFS-e
- `tpChaveRPS` - Chave identificadora de um RPS
- `tpChaveNFe` - Chave de identificação da NFS-e
- `tpChaveNFeRPS` - Chave de NFS-e e RPS
- `tpCPFCNPJ` - Tipo CPF/CNPJ
- `tpCPFCNPJNIF` - Tipo CPF/CNPJ/NIF
- `tpEndereco` - Tipo Endereço
- `tpEnderecoExterior` - Tipo endereço no exterior
- `tpInformacoesLote` - Informações do lote processado
- `tpEvento` - Tipo que representa eventos

## Usage Examples

See `XmlSerializationExamples.cs` for examples of:
- How to create and serialize `PedidoEnvioLoteRPS`
- How to deserialize `RetornoEnvioLoteRPS`

## Notes

- All classes are in the namespace `Microled.Nfe.Service.Infra.XmlSchemas`
- Classes use proper XML serialization attributes (`[XmlRoot]`, `[XmlElement]`, `[XmlAttribute]`, etc.)
- Namespaces are correctly set to match the XSD definitions
- Some complex types (IBSCBS, atvEvento, etc.) are marked with TODO for future implementation if needed

## Future Enhancements

If additional fields from the XSDs are needed, they can be added to the existing classes. The structure is designed to be extensible.

