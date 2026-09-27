# NINA.Sequencer.Generators Architecture

## Purpose

`NINA.Sequencer.Generators` is the Roslyn source-generator project used by `NINA.Sequencer`. It removes repetitive boilerplate for expression-backed sequence properties.

Build shape from `NINA.Sequencer.Generators.csproj`:

- Target framework: `netstandard2.0`
- Output type: analyzer/source-generator library

## What It Generates

The project currently contains a single generator:

- `ExpressionGenerator.cs`

This generator scans for:

- partial properties
- annotated with `[IsExpression]`

and generates partial class code that adds:

- the backing `Expression` object
- generated property accessors
- optional validator hooks
- clone support for the generated expression fields
- proxy/default/range handling encoded in attribute arguments
- a private `ValidateOwnExpressions(IList<string> issues)` helper for the class's annotated properties

## Discovery Rules

The generator is explicit about what it accepts:

- the syntax node must be a partial property declaration
- the property must use `get;` / `set;` style accessors without bodies
- the property must carry `NINA.Sequencer.Generators.IsExpressionAttribute`

It also requires the containing class to have `[UsesExpressions]`. If that attribute is missing, the generator emits diagnostic `EXP0001` and skips generation for that property.

## Generated Contract

The generated code assumes a class-level pattern used in `NINA.Sequencer`:

- a generated `Clone()` method that copies expression state
- optional partial validator methods such as `PropertyExpressionValidator(...)`
- optional partial `AfterClone(...)` hooks

This means the generator is part of the sequencer entity programming model, not just a build convenience.

## Dependency Position

The project is referenced by `NINA.Sequencer.csproj` as:

- `ProjectReference ... OutputItemType="Analyzer"`

No runtime project should instantiate or call this library directly. Its output only exists at compile time.

## Contribution Notes

### Composing expression validation

Every `[UsesExpressions]` entity gets a private `ValidateOwnExpressions(IList<string> issues)` helper. It validates the annotated properties declared by that class and appends errors to the supplied list, honoring `ValidateWhen`. It does not clear the list, assign `Issues`, call preparation or additional-validation hooks, validate children or call base validation. Containers with handwritten `Validate()` overrides call it where their explicit expression list used to be, preserving their existing preparation and child-validation order. Adding another annotated property then automatically includes it in that validation pass.

The helper is also used by generated `Validate()`. In that mode it performs the same proxy synchronization as scalar reads. Plain `[UsesExpressions]` retains its existing property and proxy behavior; the helper does not opt the entity into generated `Validate()` or `Issues`.

### Generated validation (opt-in)

Use `[UsesExpressions(GenerateValidation = true)]` on new expression entities. This adds the existing `IValidatable` contract, a public `Validate()` method and optional partial hooks:

```csharp
[UsesExpressions(GenerateValidation = true)]
public partial class Example : SequenceItem {
    [IsExpression(Default = 100, Range = [1, 100], ValidateWhen = nameof(IsROI))]
    public partial double ROIPct { get; set; }

    public bool IsROI { get; set; }

    partial void PrepareExpressionValidation() {
        // Refresh device metadata needed by expression validators.
    }

    partial void ValidateAdditional(IList<string> issues) {
        // Add device, file or other domain errors to this validation pass.
    }
}
```

The snippet omits the usual constructors and execution method. Validation creates a fresh issue list, calls preparation, validates every annotated property using `Expression.ValidateExpressions`, calls domain validation and assigns `Issues`. The generator owns `Issues`: it emits an initialized, notifying public `IList<string>` property with `[JsonIgnore]`, or reuses a compatible inherited property without redeclaring it. Remove local handwritten `Issues` declarations when opting in. An inherited property must have a public getter and an accessible non-init setter. Its setter behavior is preserved. Generated setters always retain the assigned list without copying it. Validation publishes a fresh mutable list, supporting coordinate-derived instructions that call `Issues.Clear()` and `Issues.Add()`.

`ValidateWhen` names a readable instance bool property and is checked on every pass. An inactive expression is omitted from validation only; reading its numeric property still evaluates it. Existing warning behavior, including an unexecuted variable warning, is preserved.

In this mode, `Proxy = "Data.Offset"` generates synchronization when an expression value changes, when it is validated and when its scalar property is read. Synchronization only writes values with no expression error. `HasValidator = true` remains available for domain checks before synchronization; a pure copy callback is unnecessary. Replacing an expression reattaches its context and validator. Read the scalar property in runtime code. Before passing an entire data object to a helper that consumes its cached offset, read `Offset` to refresh it.

The generator does not rewrite lifecycle methods. Keep attachment-time validation where needed, especially before starting a condition watchdog. In generated-validation mode, creation, replacement and cloning already bind each expression's `Context` to its owner; attachment code does not need to repeat those assignments. JSON property names, defaults, clone hooks and existing interfaces remain unchanged.

Diagnostics reject a handwritten `Validate()` conflict (`EXP0002`), invalid `ValidateWhen` (`EXP0003`), incompatible proxy (`EXP0004`), incompatible inherited `Issues` (`EXP0005`), inherited validation whose composition is ambiguous (`EXP0006`), a local handwritten `Issues` declaration (`EXP0007`) or a handwritten member named `ValidateOwnExpressions` (`EXP0008`). The helper-name and `ValidateWhen` checks also apply to plain `[UsesExpressions]`, since its helper is generated too. The `EXP0100` analyzer warns about direct expression-value and proxy-cache reads in opted-in entities. Validator and deserialization callbacks intentionally use cached state; other deliberate cache reads require a narrowly justified suppression. This analyzer does not perform interprocedural data-flow analysis and cannot detect a cache hidden behind an alias or another helper.

Five built-in containers retain explicit validation composition: Conditional Container, Auto Brightness Flat, Auto Exposure Flat, Smart Exposure and Take Many Exposures. Their overrides aggregate child validation and call `ValidateOwnExpressions` for their own expressions. `ExpressionBackedEntityContractTest` keeps this exception list explicit, requires factories for new entities and rejects malformed input for every annotated property. It also exercises expression context binding before attachment and through replacement, cloning and reparenting. `ExpressionGeneratorTest` compiles generated code and checks helper composition, declaration diagnostics and analyzer behavior. Neither integration test path manually evaluates expressions to repair production wiring.

- If you change the generated shape, inspect the sequence entities that rely on `Clone()`, validator hooks, and generated expression properties.
- Keep diagnostics specific and conservative; generator failures should not silently produce invalid runtime behavior.
- Add new attributes or generation modes here only when the sequencer project truly needs a repeated compile-time pattern.
