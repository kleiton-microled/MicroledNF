using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;
using Microled.Nfe.Service.Infra.Exceptions;

namespace Microled.Nfe.Service.Infra.Services;

public class ConsultaNfeXsdValidator
{
    private const string SchemaFolderRelativePath = "XSD/schemas-reformatributaria-v02-3";
    private const string RootSchemaFileName = "PedidoConsultaNFe_v02.xsd";

    private readonly ILogger<ConsultaNfeXsdValidator> _logger;
    private XmlSchemaSet? _schemaSet;

    public ConsultaNfeXsdValidator(ILogger<ConsultaNfeXsdValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Validate(string xmlContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlContent);

        var schemaSet = _schemaSet ??= CreateSchemaSet();
        var validationErrors = new List<string>();

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet,
            DtdProcessing = DtdProcessing.Prohibit
        };
        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
        settings.ValidationEventHandler += (_, args) =>
        {
            var lineInfo = args.Exception == null
                ? string.Empty
                : $" (line {args.Exception.LineNumber}, position {args.Exception.LinePosition})";

            validationErrors.Add($"{args.Severity}: {args.Message}{lineInfo}");
        };

        using var stringReader = new StringReader(xmlContent);
        using var xmlReader = XmlReader.Create(stringReader, settings);

        while (xmlReader.Read())
        {
        }

        if (validationErrors.Count > 0)
        {
            var message = $"PedidoConsultaNFe failed XSD validation: {string.Join(" | ", validationErrors)}";
            _logger.LogError(message);
            throw new NfeSoapException(message);
        }
    }

    private XmlSchemaSet CreateSchemaSet()
    {
        var schemaDirectory = ResolveSchemaDirectory();
        var schemaSet = new XmlSchemaSet();

        foreach (var filePath in Directory.GetFiles(schemaDirectory, "*.xsd"))
        {
            schemaSet.Add(null, filePath);
        }

        schemaSet.Compile();
        _logger.LogInformation("ConsultaNFe XSD validation loaded from {SchemaDirectory}", schemaDirectory);
        return schemaSet;
    }

    private string ResolveSchemaDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, SchemaFolderRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (Directory.Exists(candidate))
            {
                var rootSchemaPath = Path.Combine(candidate, RootSchemaFileName);
                if (File.Exists(rootSchemaPath))
                {
                    return candidate;
                }
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate '{SchemaFolderRelativePath}' starting from '{AppContext.BaseDirectory}'.");
    }
}
