using EvolveUI.Parsing;

public class Program {

    public static unsafe void Main(string[] args) {

        string filePath = Path.GetFullPath("../../../../TemplateExamples/App.ui");
        
        string source = File.ReadAllText(filePath);

        if (TemplateParser.TryParseTemplate(filePath, source, out TemplateParseResult result)) {
            Console.WriteLine(TemplatePrintingVisitor.Print(source, result));
        }
        else {
            
            for (int i = 0; i < result.errors.Length; i++) {
                Console.WriteLine(result.errors[i].message);
            }
        }
        
    }

}
