using FluentAssertions;
using Microled.Nfe.Service.Application.Services;
using Xunit;

namespace Microled.Nfe.Service.Tests.Application;

public class RetornoXmlParserTests
{
  private const string SpNfeXml = """
    <NFe xmlns="">
      <TipoRPS>RPS</TipoRPS>
      <StatusNFe>N</StatusNFe>
      <TributacaoNFe>T</TributacaoNFe>
      <ValorServicos>2300.65</ValorServicos>
      <CodigoServico>1023</CodigoServico>
      <AliquotaServicos>0.05</AliquotaServicos>
      <ValorISS>115.03</ValorISS>
      <ValorDeducoes>10.00</ValorDeducoes>
      <ValorPIS>1.11</ValorPIS>
      <ValorCOFINS>2.22</ValorCOFINS>
      <ValorINSS>3.33</ValorINSS>
      <ValorIR>4.44</ValorIR>
      <ValorCSLL>5.55</ValorCSLL>
      <ISSRetido>false</ISSRetido>
      <RazaoSocialTomador>TERMARES TERMINAIS MARITIMOS ESPECIALIZADOS LTDA</RazaoSocialTomador>
      <EmailTomador>contato@termares.com.br</EmailTomador>
      <InscricaoMunicipalTomador>12345678</InscricaoMunicipalTomador>
      <EnderecoTomador>
        <TipoLogradouro>RUA</TipoLogradouro>
        <Logradouro>TESTE</Logradouro>
        <NumeroEndereco>0001</NumeroEndereco>
        <ComplementoEndereco>0101</ComplementoEndereco>
        <Bairro>CENTRO</Bairro>
        <Cidade>3550308</Cidade>
        <UF>SP</UF>
        <CEP>4141500</CEP>
      </EnderecoTomador>
      <Discriminacao>teste geral IBS</Discriminacao>
      <IBSCBS>
        <indDest>1</indDest>
        <valores>
          <trib>
            <gIBSCBS>
              <cClassTrib>000001</cClassTrib>
            </gIBSCBS>
          </trib>
        </valores>
      </IBSCBS>
      <RetornoComplementarIBSCBS>
        <ValorAliqEstadualIBS>0.3</ValorAliqEstadualIBS>
        <ValorAliqMunicipalIBS>0.07</ValorAliqMunicipalIBS>
        <ValorAliqCBS>0.1</ValorAliqCBS>
      </RetornoComplementarIBSCBS>
    </NFe>
    """;

    [Fact]
    public void ExtractRpsContentFields_FromSpNfe_ShouldPopulateServiceTomadorAndTaxes()
    {
        var fields = RetornoXmlParser.ExtractRpsContentFields(SpNfeXml);

        fields.TipoRps.Should().Be("RPS");
        fields.StatusRps.Should().Be("N");
        fields.TributacaoRps.Should().Be("T");
        fields.Discriminacao.Should().Be("teste geral IBS");
        fields.CodigoServico.Should().Be("1023");
        fields.ValorServicos.Should().Be("2300.65");
        fields.AliquotaServicos.Should().Be("0.05");
        fields.IssRetido.Should().Be("false");
        fields.ValorIss.Should().Be("115.03");
        fields.ValorDeducoes.Should().Be("10.00");
        fields.ValorPis.Should().Be("1.11");
        fields.ValorCofins.Should().Be("2.22");
        fields.ValorInss.Should().Be("3.33");
        fields.ValorIr.Should().Be("4.44");
        fields.ValorCsll.Should().Be("5.55");
        fields.TomadorEmail.Should().Be("contato@termares.com.br");
        fields.TomadorInscricaoMunicipal.Should().Be("12345678");
        fields.TomadorCep.Should().Be("4141500");
        fields.TomadorLogradouro.Should().Be("RUA TESTE");
        fields.TomadorNumero.Should().Be("0001");
        fields.TomadorComplemento.Should().Be("0101");
        fields.TomadorBairro.Should().Be("CENTRO");
        fields.TomadorCodigoMunicipio.Should().Be("3550308");
        fields.TomadorUf.Should().Be("SP");
        fields.IbsIndDest.Should().Be("1");
        fields.IbsCstIbs.Should().Be("000001");
        fields.IbsAliqEstadual.Should().Be("0.3");
        fields.IbsAliqMunicipal.Should().Be("0.07");
        fields.IbsAliqCbs.Should().Be("0.1");
    }

    [Fact]
    public void ExtractRazaoSocialTomador_FromSpNfe_ShouldReturnTomadorName()
    {
        var nome = RetornoXmlParser.ExtractRazaoSocialTomador(SpNfeXml);

        nome.Should().Be("TERMARES TERMINAIS MARITIMOS ESPECIALIZADOS LTDA");
    }

    [Fact]
    public void ExtractRpsContentFields_FromRetornoConsultaWrapper_ShouldFindInnerNFe()
    {
        var xml = $"""
            <RetornoConsulta xmlns="http://www.prefeitura.sp.gov.br/nfe">
              <Cabecalho Versao="2" xmlns="">
                <Sucesso>true</Sucesso>
              </Cabecalho>
              {SpNfeXml}
            </RetornoConsulta>
            """;

        var fields = RetornoXmlParser.ExtractRpsContentFields(xml);

        fields.ValorServicos.Should().Be("2300.65");
        fields.CodigoServico.Should().Be("1023");
        RetornoXmlParser.ExtractRazaoSocialTomador(xml)
            .Should().Be("TERMARES TERMINAIS MARITIMOS ESPECIALIZADOS LTDA");
    }

    [Fact]
    public void ExtractRpsContentFields_FromSpRps_ShouldPopulateTomadorInscricaoMunicipal()
    {
        const string xml = """
            <RPS xmlns="">
              <InscricaoMunicipalTomador>99887766</InscricaoMunicipalTomador>
              <RazaoSocialTomador>EMPRESA TESTE</RazaoSocialTomador>
              <CodigoServico>2919</CodigoServico>
              <ValorFinalCobrado>1500.00</ValorFinalCobrado>
            </RPS>
            """;

        var fields = RetornoXmlParser.ExtractRpsContentFields(xml);

        fields.TomadorInscricaoMunicipal.Should().Be("99887766");
        fields.ValorServicos.Should().Be("1500.00");
    }

    [Fact]
    public void ExtractRpsContentFields_FromAbrasfTomadorIdentificacao_ShouldPopulateTomadorInscricaoMunicipal()
    {
        const string xml = """
            <EnviarLoteRpsEnvio>
              <LoteRps>
                <ListaRps>
                  <Rps>
                    <InfDeclaracaoPrestacaoServico>
                      <TomadorServico>
                        <IdentificacaoTomador>
                          <InscricaoMunicipal>55443322</InscricaoMunicipal>
                          <CpfCnpj><Cnpj>12345678000190</Cnpj></CpfCnpj>
                        </IdentificacaoTomador>
                      </TomadorServico>
                    </InfDeclaracaoPrestacaoServico>
                  </Rps>
                </ListaRps>
              </LoteRps>
            </EnviarLoteRpsEnvio>
            """;

        var fields = RetornoXmlParser.ExtractRpsContentFields(xml);

        fields.TomadorInscricaoMunicipal.Should().Be("55443322");
    }

    [Fact]
    public void ExtractRpsContentFields_ShouldNotConfusePrestadorInscricaoMunicipalWithTomador()
    {
        const string xml = """
            <NFe xmlns="">
              <ChaveNFe>
                <InscricaoPrestador>37684280</InscricaoPrestador>
              </ChaveNFe>
              <Prestador>
                <InscricaoMunicipal>37684280</InscricaoMunicipal>
              </Prestador>
              <CPFCNPJTomador><CNPJ>53730495000170</CNPJ></CPFCNPJTomador>
              <RazaoSocialTomador>TERMARES</RazaoSocialTomador>
            </NFe>
            """;

        var fields = RetornoXmlParser.ExtractRpsContentFields(xml);

        fields.TomadorInscricaoMunicipal.Should().BeNull();
    }

    [Fact]
    public void ExtractRpsContentFields_FromPrefeituraConsultaExample_ShouldNotHaveTomadorInscricaoMunicipal()
    {
        const string xml = """
            <NFe xmlns="">
              <CPFCNPJTomador><CNPJ>99999999000166</CNPJ></CPFCNPJTomador>
              <RazaoSocialTomador>TESTE</RazaoSocialTomador>
              <ValorServicos>2300.65</ValorServicos>
              <CodigoServico>1023</CodigoServico>
            </NFe>
            """;

        var fields = RetornoXmlParser.ExtractRpsContentFields(xml);

        fields.TomadorInscricaoMunicipal.Should().BeNull();
        fields.ValorServicos.Should().Be("2300.65");
    }
}
