using Microled.Nfe.Service.Application.Services;

namespace Microled.Nfe.Service.Tests.Application;

public class RetornoEnvioLoteRpsResultadoParserTests
{
    [Fact]
    public void ParseRpsEvents_ShouldExtractErroWithChaveRps_FromEmbeddedRetorno()
    {
        const string xml = """
            <RetornoEnvioLoteRPS xmlns="http://www.prefeitura.sp.gov.br/nfe">
              <Erro>
                <Codigo>641</Codigo>
                <Descricao>Contribuinte cadastrado como Simples Nacional</Descricao>
                <ChaveRPS>
                  <InscricaoPrestador>1555553</InscricaoPrestador>
                  <SerieRPS>A</SerieRPS>
                  <NumeroRPS>2497</NumeroRPS>
                </ChaveRPS>
              </Erro>
            </RetornoEnvioLoteRPS>
            """;

        var events = RetornoEnvioLoteRpsResultadoParser.ParseRpsEvents(xml);

        Assert.Single(events);
        Assert.True(events[0].IsErro);
        Assert.Equal("641", events[0].Codigo);
        Assert.Equal("1555553", events[0].InscricaoPrestador);
        Assert.Equal("A", events[0].SerieRps);
        Assert.Equal("2497", events[0].NumeroRps);
    }
}
