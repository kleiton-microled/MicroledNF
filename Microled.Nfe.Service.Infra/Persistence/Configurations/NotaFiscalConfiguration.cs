using Microled.Nfe.Service.Domain.Entities;
using Microled.Nfe.Service.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microled.Nfe.Service.Infra.Persistence.Configurations;

public sealed class NotaFiscalConfiguration : IEntityTypeConfiguration<NotaFiscal>
{
    public void Configure(EntityTypeBuilder<NotaFiscal> builder)
    {
        builder.ToTable("notas_fiscais");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Protocolo)
            .HasColumnName("protocolo")
            .HasMaxLength(100);

        builder.Property(x => x.NumeroNota)
            .HasColumnName("numero_nota")
            .HasMaxLength(50);

        builder.Property(x => x.CodigoVerificacao)
            .HasColumnName("codigo_verificacao")
            .HasMaxLength(50);

        builder.Property(x => x.NumeroLote)
            .HasColumnName("numero_lote")
            .HasMaxLength(50);

        builder.Property(x => x.NumeroRps)
            .HasColumnName("numero_rps")
            .HasMaxLength(50);

        builder.Property(x => x.SerieRps)
            .HasColumnName("serie_rps")
            .HasMaxLength(20);

        builder.Property(x => x.InscricaoPrestador)
            .HasColumnName("inscricao_prestador")
            .HasMaxLength(20);

        builder.Property(x => x.CnpjPrestador)
            .HasColumnName("cnpj_prestador")
            .HasMaxLength(14);

        builder.Property(x => x.CpfCnpjTomador)
            .HasColumnName("cpf_cnpj_tomador")
            .HasMaxLength(14);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Xml)
            .HasColumnName("xml")
            .HasColumnType("text");

        builder.Property(x => x.Pdf)
            .HasColumnName("pdf")
            .HasColumnType("bytea");

        builder.Property(x => x.DataEmissao)
            .HasColumnName("data_emissao");

        builder.Property(x => x.DataCancelamento)
            .HasColumnName("data_cancelamento");

        builder.Property(x => x.CriadoPor)
            .HasColumnName("criado_por")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.AlteradoPor)
            .HasColumnName("alterado_por")
            .HasMaxLength(100);

        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(x => x.AlteradoEm)
            .HasColumnName("alterado_em");

        builder.HasIndex(x => x.Protocolo);
        builder.HasIndex(x => x.NumeroNota);
        builder.HasIndex(x => new { x.InscricaoPrestador, x.SerieRps, x.NumeroRps });
        builder.HasIndex(x => x.CnpjPrestador);
        builder.HasIndex(x => x.CpfCnpjTomador);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DataEmissao);
        builder.HasIndex(x => x.CriadoEm);
    }
}
