using Microled.Nfe.Service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Microled.Nfe.Service.Infra.Persistence.Configurations;

public sealed class TomadorConfiguration : IEntityTypeConfiguration<Tomador>
{
    public void Configure(EntityTypeBuilder<Tomador> builder)
    {
        builder.ToTable("tomadores");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CpfCnpj)
            .HasColumnName("cpf_cnpj")
            .HasMaxLength(14)
            .IsRequired();
        builder.Property(x => x.RazaoSocial)
            .HasColumnName("razao_social")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(x => x.InscricaoMunicipal)
            .HasColumnName("inscricao_municipal")
            .HasMaxLength(20);
        builder.Property(x => x.InscricaoEstadual)
            .HasColumnName("inscricao_estadual")
            .HasMaxLength(20);
        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(200);
        builder.Property(x => x.TipoLogradouro)
            .HasColumnName("tipo_logradouro")
            .HasMaxLength(20);
        builder.Property(x => x.Logradouro)
            .HasColumnName("logradouro")
            .HasMaxLength(200);
        builder.Property(x => x.Numero)
            .HasColumnName("numero")
            .HasMaxLength(20);
        builder.Property(x => x.Complemento)
            .HasColumnName("complemento")
            .HasMaxLength(100);
        builder.Property(x => x.Bairro)
            .HasColumnName("bairro")
            .HasMaxLength(100);
        builder.Property(x => x.Uf)
            .HasColumnName("uf")
            .HasMaxLength(2);
        builder.Property(x => x.CodigoMunicipio)
            .HasColumnName("codigo_municipio")
            .HasMaxLength(10);
        builder.Property(x => x.Cep)
            .HasColumnName("cep")
            .HasMaxLength(8);
        builder.Property(x => x.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();
        builder.Property(x => x.AlteradoEm)
            .HasColumnName("alterado_em");

        builder.HasIndex(x => x.CpfCnpj).IsUnique();
        builder.HasIndex(x => x.RazaoSocial);
    }
}
