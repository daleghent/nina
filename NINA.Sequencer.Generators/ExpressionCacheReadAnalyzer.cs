using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace NINA.Sequencer.Generators {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExpressionCacheReadAnalyzer : DiagnosticAnalyzer {
        private static readonly DiagnosticDescriptor CacheRead = new("EXP0100", "Expression cache bypasses evaluation",
            "Read '{0}' to evaluate the expression before consuming its value; '{1}' is cached state",
            "Usage", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CacheRead);

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeRead, SyntaxKind.SimpleMemberAccessExpression);
        }

        private static void AnalyzeRead(SyntaxNodeAnalysisContext context) {
            var access = (MemberAccessExpressionSyntax)context.Node;
            var owner = context.SemanticModel.GetEnclosingSymbol(access.SpanStart);
            while (owner is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction }) owner = owner.ContainingSymbol;
            if (owner is not IMethodSymbol && owner is not IPropertySymbol) return;
            if (owner is IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor }) return;
            var type = owner.ContainingType;
            if (!ExpressionGenerator.GeneratesValidation(type)) return;
            // Validator callbacks deliberately inspect cached values to avoid recursive evaluation.
            // Deserialization callbacks inspect the saved proxy before migrating legacy JSON.
            if (owner.Name.EndsWith("ExpressionValidator", System.StringComparison.Ordinal)
                || owner.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.Runtime.Serialization.OnDeserializedAttribute")) return;
            if (access.Parent is AssignmentExpressionSyntax assignment && assignment.Left == access) return;
            if (access.Ancestors().OfType<InvocationExpressionSyntax>().Any(i => i.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" })) return;

            foreach (var property in type.GetMembers().OfType<IPropertySymbol>()) {
                var attribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "NINA.Sequencer.Generators.IsExpressionAttribute");
                if (attribute == null) continue;
                var proxy = attribute.NamedArguments.FirstOrDefault(a => a.Key == "Proxy").Value.Value as string;
                var text = access.ToString();
                if (text.StartsWith("this.", System.StringComparison.Ordinal)) text = text.Substring(5);
                var cachedValue = access.Name.Identifier.ValueText == "Value"
                    && context.SemanticModel.GetSymbolInfo(access.Expression).Symbol is IPropertySymbol expression
                    && SymbolEqualityComparer.Default.Equals(expression.ContainingType, type)
                    && expression.Name == property.Name + "Expression";
                ExpressionSyntax root = access;
                while (root is MemberAccessExpressionSyntax member && member.Expression is not ThisExpressionSyntax) root = member.Expression;
                var ownProxy = proxy != null && text == proxy && SymbolEqualityComparer.Default.Equals(
                    context.SemanticModel.GetSymbolInfo(root).Symbol, ExpressionGenerator.FindMember(type, proxy.Split('.')[0]));
                if (cachedValue || ownProxy) {
                    context.ReportDiagnostic(Diagnostic.Create(CacheRead, access.GetLocation(), property.Name, text));
                    return;
                }
            }
        }
    }
}
