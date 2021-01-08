package com.javaconvertter.demo.visitors;

import com.github.javaparser.JavaToken;
import com.github.javaparser.Position;
import com.github.javaparser.ast.*;
import com.github.javaparser.ast.body.*;
import com.github.javaparser.ast.comments.JavadocComment;
import com.github.javaparser.ast.expr.*;
import com.github.javaparser.ast.nodeTypes.NodeWithJavadoc;
import com.github.javaparser.ast.stmt.CatchClause;
import com.github.javaparser.ast.stmt.ForEachStmt;
import com.github.javaparser.ast.type.ClassOrInterfaceType;
import com.github.javaparser.ast.type.TypeParameter;
import com.github.javaparser.ast.type.UnionType;
import com.github.javaparser.ast.visitor.GenericVisitorAdapter;
import com.github.javaparser.ast.visitor.Visitable;
import com.github.javaparser.javadoc.JavadocBlockTag;
import com.github.javaparser.javadoc.description.JavadocDescription;
import com.github.javaparser.javadoc.description.JavadocInlineTag;
import com.google.common.collect.Streams;
import com.javaconvertter.demo.RuleBuilder;
//import org.reflections8.Reflections;
//import org.reflections8.scanners.ResourcesScanner;
//import org.reflections8.scanners.SubTypesScanner;
//import org.reflections8.util.ClasspathHelper;
//import org.reflections8.util.ConfigurationBuilder;
//import org.reflections8.util.FilterBuilder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.*;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class CSharpVisitor extends GenericVisitorAdapter<Visitable, RuleBuilder> {
    private static final Logger logger = LoggerFactory.getLogger(CSharpVisitor.class);
    public List<String> originalImports;

    public CSharpVisitor(List<String> originalImports) {
        this.originalImports = originalImports;
    }

    @Override
    public Visitable visit(CompilationUnit n, RuleBuilder rules) {
        var namespace = n.getPackageDeclaration().get().getName();
        //var firstNonUsingNode = n.getChildNodes().stream().filter(x -> !(x instanceof PackageDeclaration) && (!(x instanceof )))
        var lastImport = n.getImports().getLast().map(x -> (Node)x).or(() -> n.getPackageDeclaration()).get();
        var namespaceDeclaration = String.format("\n\nnamespace %s\n{", namespace);


//        rules.replace(n, n.getChildNodes().get(n.getChildNodes().size()-2), result); // last using statement
        rules.append(lastImport, namespaceDeclaration); // last using statement
        rules.append(n, "\n}\n");
        return super.visit(n, rules);
    }

    @Override
    public Visitable visit(InitializerDeclaration n, RuleBuilder rules) {
        if(n.isStatic()){
            rules.append(findTokenIn(n, JavaToken.Kind.STATIC).get(), " " + ((ClassOrInterfaceDeclaration)n.getParentNode().get()).getName() + "()");
        }
        return super.visit(n, rules);
    }

    @Override
    public Visitable visit(MethodCallExpr n, RuleBuilder rules) {
        n.getTypeArguments().ifPresent(x -> {
            rules.delete(n.getName());
            rules.prepend(findTokenLeftOf(n.getName(), JavaToken.Kind.LT).get(), n.getName().asString());
        });
        return super.visit(n, rules);
    }

    @Override
    public Visitable visit(ImportDeclaration n, RuleBuilder rules) {
        rules.replace(findTokenLeftOf(n.getName(), JavaToken.Kind.IMPORT).get(), "using");
        return super.visit(n, rules);
    }
    @Override
    public Visitable visit(PackageDeclaration n, RuleBuilder rules) {
        rules.delete(n);
        return super.visit(n,rules);
    }
    @Override
    public Visitable visit(SuperExpr n, RuleBuilder rules) {
        rules.replace(findTokenIn(n, JavaToken.Kind.SUPER).get(), "base");
        return super.visit(n,rules);
    }
    @Override
    public Visitable visit(InstanceOfExpr n, RuleBuilder rules) {
        rules.replace(findTokenIn(n, JavaToken.Kind.INSTANCEOF).get(), "is");
        return super.visit(n, rules);
    }
    

    // public Blah()
    @Override
    public Visitable visit(ConstructorDeclaration n, RuleBuilder rules) {
        convertJavadoc(n, rules);
        n.getBody()
                .getStatements()
                .getFirst()
                .ifPresent(x -> {
                    if(x.isExplicitConstructorInvocationStmt()){
                        var base = x.asExplicitConstructorInvocationStmt();
//                        String baseStr = trim(base.toString(), ";");
                        if(!base.isThis()) {
                            rules.replace(findTokenIn(base, JavaToken.Kind.SUPER).get(), "base");
//                            baseStr = baseStr.replaceAll("^super", " : base");
                        }
                        rules.replace(findTokenLeftOf(base, JavaToken.Kind.LBRACE).get(), ":");
                        rules.replace(findTokenIn(base, JavaToken.Kind.SEMICOLON).get(), "{");
//                        rules.delete(base);
//                        rules.prepend(findTokenIn(n, JavaToken.Kind.LBRACE).get(), baseStr);
                    }
                });
        return super.visit(n, rules);
    }
    // Type::Method
    @Override

    public Visitable visit(MethodReferenceExpr n, RuleBuilder rules) {
//        n.getId()

        var scope = n.getScope().toString();
        var target = n.getIdentifier();

        var pName = String.format("p%s", getDepth(n));
        if(scope.equals("Objects") && target.equals("nonNull")){
            rules.replace(n, String.format("%s => %s != null", pName, pName));
            return super.visit(n, rules);
        }

//        rules.replace(n, scope + "." + target);

        var imports = Stream.concat(
                originalImports.stream(),
                Stream.of("java.lang.*"))
                .collect(Collectors.toList());
        var classType = imports.stream()
                .filter(x -> x.endsWith("." + scope))
                .flatMap(x -> {
                    try {
                        return Stream.of((Class)Class.forName(x));
                    }catch(ClassNotFoundException e){
                        logger.error(String.format("Class %s can't be loaded", x));
                        return Stream.empty();
                    }
                })
                .findFirst()
                .or(() -> imports
                        .stream()
                        .filter(x -> x.endsWith("*"))
                        .flatMap(x -> {
                            var namespace = x.substring(0, x.length()-1);
                            try {
                                return Stream.of(Class.forName(namespace + scope));
                            }catch(ClassNotFoundException e){
                                return Stream.empty();
                            }
                        })
                        .findFirst()
                );
        AtomicBoolean done = new AtomicBoolean(false);
        if(classType.isPresent()){

            var method = Arrays.stream(classType.get().getMethods()).filter(x -> x.getName().equals(target)).findFirst();
            method.ifPresent(m -> {
                var isStatic = java.lang.reflect.Modifier.isStatic(m.getModifiers());
                if(isStatic){
                    done.set(true);
                    var depth = getDepth(n);
                    rules.replace(n, String.format("p%s => %s.%s(p%s)", depth, scope, target, depth));
                }
            });
        }
        if(!done.get()) {
            var doubleColon = findTokenRightOf(n.getScope(), JavaToken.Kind.DOUBLECOLON).get();
            rules.replace(doubleColon, ".");
        }
        return super.visit(n, rules);
    }

    private int getDepth(Node node){
        var depth = 0;
        var current = node.getParentNode();
        while(current.isPresent()){
            depth++;
            current = current.get().getParentNode();
        }
        return depth;
    }

    // public class Blah
    @Override
    public Visitable visit(ClassOrInterfaceDeclaration n, RuleBuilder rules) {
        convertJavadoc(n, rules);

        var start = new Position(n.getName().getBegin().get().line, 1);
        var stop = findTokenRightOf(n.getName(), JavaToken.Kind.LBRACE).get().getRange().get().begin;

        var csharp = declareClass(n);
        rules.replace(start, stop, csharp);
        return super.visit(n, rules);
    }

    // public @interface Blah
    @Override
    public Visitable visit(AnnotationDeclaration n, RuleBuilder rules) {

        convertJavadoc(n, rules);
        var declaration = "public class " + n.getNameAsString() + " : Attribute";
        var start = new Position(n.getName().getBegin().get().line, 1);
        var stop = this.getPositionAtClosingBracket(n, n.getName());
        rules.replace(start, stop, declaration);
        return super.visit(n, rules);
    }

    // property inside attribute class
    @Override
    public Visitable visit(AnnotationMemberDeclaration n, RuleBuilder rules) {
        convertJavadoc(n, rules);

        n.getDefaultValue()
            .ifPresentOrElse(defaultExpresison ->
                rules.replace(findTokenLeftOf(defaultExpresison, JavaToken.Kind._DEFAULT).get(), "="),
            () -> rules.delete(findTokenRightOf(n.getName(), JavaToken.Kind.SEMICOLON).get()));
        rules.builder()
                .fromEndOf(n.getName())
                .toEndOf(findTokenRightOf(n.getName(), JavaToken.Kind.RPAREN).get())
                .replace(" { get; set; }");

        return super.visit(n, rules);
    }

    @Override
    public Visitable visit(CatchClause n, RuleBuilder rules) {
        if(n.getParameter().getType() instanceof UnionType)
        {

//            n.getParameter().getType()
            var union = (UnionType)n.getParameter().getType();
            var types = union.getElements().stream()
                    .map(x -> "ex is " + x.toString())
                    .collect(Collectors.joining(" || "));
            rules.prepend(findTokenIn(n, JavaToken.Kind.LBRACE).get(), " when ( " + types + " )");
            rules.replace(n.getParameter(), "Exception ex");
//
//            var newParameter = n.getParameter().setType(new ClassOrInterfaceType(null, "Exception"));
//            var c = n.setParameter(newParameter);
        }
        return super.visit(n, rules);
    }

    // Visitable Method()
    @Override
    public Visitable visit(MethodDeclaration n, RuleBuilder rules) {
        convertJavadoc(n, rules);

        n.getParameters().getLast().ifPresent(lastParameter -> {
            if(!lastParameter.isVarArgs())
                return;
            rules.prepend(lastParameter.getType(), "params ");
            rules.replace(findTokenRightOf(lastParameter.getType(), JavaToken.Kind.ELLIPSIS).get(),"[]");
        });
        // move type parameters after method name
        n.getTypeParameters().ifNonEmpty(typeParameters -> {
            rules.builder()
                    .fromStartOf(findTokenLeftOf(typeParameters.getFirst().get(), JavaToken.Kind.LT).get())
                    .toEndOf(findTokenLeftOf(n.getType(), JavaToken.Kind.GT).get())
                    .delete();

            var typeParamsString = this.getTypeParameters(typeParameters);
            var constraints = this.getGenericsConstraints(typeParameters);

            rules.append(n.getName(), typeParamsString);

            var tokenKind = n.getBody().map(x -> JavaToken.Kind.LBRACE).orElse( JavaToken.Kind.SEMICOLON);
            var token = findTokenRightOf(n.getName(), tokenKind);
            if(!token.isPresent()){
                logger.error("break");
            }
            rules.prepend(findTokenRightOf(n.getName(), tokenKind).get(), constraints);
        });
        return super.visit(n, rules);
    }

    // MyType.class
    @Override
    public Visitable visit(ClassExpr n, RuleBuilder rules) {
        rules.replace(n, String.format("typeof(%s)", n.getType().asString()));
        return super.visit(n, rules);
    }

    // @Blah
    @Override
    public Visitable visit(MarkerAnnotationExpr n, RuleBuilder rules) {
        putAttributeInBrackets(n, rules);
        return super.visit(n, rules);
    }

    // @Blah("value")
    @Override
    public Visitable visit(SingleMemberAnnotationExpr n, RuleBuilder rules) {
        putAttributeInBrackets(n, rules);

        // if parameter is array (such as Target), switch it to enum flags
        n.getMemberValue().ifArrayInitializerExpr(array -> {
            var valuesAsEnumFlags = array.toString()
                    .replaceAll("[{}]","")
                    .replaceAll(",", " |");
            rules.replace(array, valuesAsEnumFlags);
        });
        return super.visit(n, rules);
    }

    @Override
    public Visitable visit(FieldDeclaration n, RuleBuilder arg) {
        convertJavadoc(n, arg);
        return super.visit(n, arg);
    }

    // @Blah(field = "value")
    @Override
    public Visitable visit(NormalAnnotationExpr n, RuleBuilder rules) {
        putAttributeInBrackets(n, rules);
        return super.visit(n, rules);
    }

    @Override
    public Visitable visit(ForEachStmt n, RuleBuilder rules) {
        rules.replace(findTokenRightOf(n.getVariable(), JavaToken.Kind.COLON).get(), "in");
        rules.replace(findTokenLeftOf(n.getVariable(), JavaToken.Kind.FOR).get(), "foreach");
        return super.visit(n, rules);
    }

    @Override
    public Visitable visit(Modifier n, RuleBuilder rules) {
        switch (n.getKeyword()){
            case FINAL:
                rules.replace(n, "readonly");
                break;
            case TRANSIENT:
                rules.prepend(n.getParentNode().get(), "[NonSerialize] ");
                rules.delete(n);
        }
        return super.visit(n, rules);
    }

    // replace javadoc with xmldoc
    private void convertJavadoc(NodeWithJavadoc n, RuleBuilder rules){
        n.getJavadocComment()
                .ifPresent(javadoc -> rules.replace((Node)javadoc, (String)n.getJavadocComment().map(x -> toXmlDoc((JavadocComment)x, (Node)n)).get()));

    }




    private void putAttributeInBrackets(AnnotationExpr n, RuleBuilder rules){
        var start = n.getBegin().get();
        rules.replace(start, start.right(1), "[").append(n, "]");
    }

    private Position getPositionAtClosingBracket(Node n) {
        return getPositionAtClosingBracket(n, null);
    }
    private Optional<JavaToken> findTokenIn(Node n, JavaToken.Kind token){
        var tokenRange = n.getTokenRange().get();

        //var current = Optional.of(tokenRange.getBegin());
        for(var current : tokenRange){
            if(JavaToken.Kind.valueOf(current.getKind()) == token){
                return Optional.of(current);
            }
        }
        return Optional.empty();
    }
    private Optional<JavaToken> findTokenRightOf(Node n, JavaToken.Kind token){
        var current = Optional.of(n.getTokenRange().get().getEnd());
        while(current.isPresent()){
            if(JavaToken.Kind.valueOf(current.get().getKind()) == token){
                return current;
            }
            current = current.get().getNextToken();
        }
        return current;
    }

    private Optional<JavaToken> findTokenLeftOf(Optional<Node> n, JavaToken.Kind token){
        return n.flatMap(x -> findTokenLeftOf(x, token));
    }
    private Optional<JavaToken> findTokenLeftOf(Node n, JavaToken.Kind token){
        var current = Optional.of(n.getTokenRange().get().getBegin());
        while(current.isPresent()){
            if(JavaToken.Kind.valueOf(current.get().getKind()) == token){
                return current;
            }
            current = current.get().getPreviousToken();
        }
        return current;

    }
    private Position getPositionAtClosingBracket(Node n, Node afterNode){

        var startPosition = afterNode != null ? afterNode.getEnd().get() : n.getRange().get().begin;
        return Streams.stream(n.getTokenRange().get())
                .filter(token -> token.getRange().get().begin.isAfter(startPosition) && token.asString() == "{")
                .map(x -> x.getRange().get().begin.right(-1))
                .findFirst()
                .get();
    }

    private String toXmlDoc(JavadocComment javadocNode, Node memberNode){
        var javadoc = javadocNode.parse();
        var ident = javadocNode.getBegin().get().column-1;
        var summary = ident(getSummary(javadoc.getDescription(), memberNode));
        var remarks = getRemarks(javadoc.getDescription(), memberNode).map(x -> ident(x));
        var sb = new StringBuilder();
        sb.append("<summary>\n");
        sb.append(summary);
        sb.append("\n</summary>\n");
        var tags = javadoc.getBlockTags().stream().map(x -> getTag(x, memberNode)).collect(Collectors.joining(""));
        sb.append(tags);
        remarks.ifPresent(x -> sb.append(String.format("\n<remarks>\n%s\n</remarks>", x)));
        var deprecated = javadoc.getBlockTags().stream().filter(x -> x.getType() == JavadocBlockTag.Type.DEPRECATED).findFirst();
        var xmlDoc = sb.toString().lines()
                .map(x -> String.format("/// %s", x))
                .collect(Collectors.joining("\n" + " ".repeat(ident)));
        var result = xmlDoc;

        if(deprecated.isPresent()){
//            var obsoleteText = getDescription(deprecated.get().getContent(), memberNode);
            var obsoleteText = "";
            result += String.format("\n%s[Obsolete(\"%s\")]", " ".repeat(ident), obsoleteText);
        }
        return result;
    }
    private String getTag(JavadocBlockTag n, Node memberNode){
        var description = getDescription(n.getContent(), memberNode);
        switch (n.getType()){
            case PARAM:
                var name = n.getName().get();
                var isGenericParameter = name.matches("^<\\w+>$");
                var tagName = "param";
                if(isGenericParameter)
                {
                    tagName = "typeparam";
                    name = name.replaceAll("[<>]","");
                }
                return String.format("<%s name=\"%s\">%s</%s>", tagName, name, description, tagName);
            case RETURN:
                return String.format("<returns>%s</returns>", description);
            case THROWS:
            case EXCEPTION:
                return String.format("<exception cref=\"%s\">%s</exception>", n.getName().get(), description);
        }
        return "";
    }

    private String getDescription(JavadocDescription n, Node memberNode){
        var paragraphs = getDescriptionParagraphs(n, memberNode);
        if(paragraphs.size() == 1)
            return paragraphs.get(0);
        return getDescriptionParagraphs(n, memberNode).stream()
                .map(x -> String.format("<para>%s</para>\n", x))
                .collect(Collectors.joining("\n"));
    }
    private String ident(String val) {
        return ident(val, 2);
    }
    private String ident(String val, int ident){
        var result = Arrays.stream(val.split("\n"))
                .map(x -> " ".repeat(ident) + x.trim())
                .collect(Collectors.joining("\n"));
        return result;
    }
    private String getSummary(JavadocDescription n, Node memberNode){
        return getDescriptionParagraphs(n, memberNode).get(0);
    }
    private Optional<String> getRemarks(JavadocDescription n, Node memberNode){
        var paragraphs = getDescriptionParagraphs(n, memberNode);
        if(paragraphs.size() == 1)
            return Optional.empty();
        if(paragraphs.size() == 2)
            return Optional.of(paragraphs.get(1));
        return Optional.of(paragraphs.stream()
                .skip(1)
                .map(x -> String.format("<para>%s</para>", x))
                .collect(Collectors.joining("\n")));
    }
    private List<String> getDescriptionParagraphs(JavadocDescription n, Node memberNode){
        return Arrays.stream(n.getElements().stream()
                .map(segment -> {
                    if(segment instanceof JavadocInlineTag){
                        return getTag((JavadocInlineTag)segment, memberNode);
                    }else{
                        return trim(segment.toText(), " ");
                    }
                })
                .collect(Collectors.joining(" "))
                .split("<p>"))
                .map(x -> x.trim())
                .collect(Collectors.toList());

    }

    private String trim(String input, String charactersToStrip){
        return input.replaceAll(String.format("(^[%s]*)|([%s]*$)", charactersToStrip, charactersToStrip), "");
    }

    private String getTag(JavadocInlineTag n, Node memberNode){
        var name = n.getContent().trim();
        switch (n.getType()){
            case LINKPLAIN:
            case LINK:
                return String.format("<see cref=\"%s\"/>", name);
            case VALUE:
                return n.getContent().trim();
            case CODE:
            case LITERAL:
//                Stream<TypeParameter> genericruless = Stream.empty();
//                if(memberNode instanceof ClassOrInterfaceDeclaration){
//                    genericruless = ((ClassOrInterfaceDeclaration) memberNode).getTypeParameters().stream();
//                }
//                else if(memberNode instanceof MethodDeclaration){
//                    genericruless = ((MethodDeclaration) memberNode).getTypeParameters().stream();
//                }
//                var rules = genericruless.map(x -> x.getNameAsString()).collect(Collectors.toList());
                var rules = Helper.getGenericsInScope(memberNode);
                var isGenericParamReference = rules.contains(name);

                return  String.format(isGenericParamReference ? "<typeparamref cref=\"%s\" />" : "<c>%s</c>", name);
            case INHERIT_DOC:
                return "<inheritdoc/>";

        }
        return "";
    }


    private String declareClass(ClassOrInterfaceDeclaration n){
        var modifiers = n.getModifiers().stream()
            .map(x -> x.getKeyword().asString())
            .collect(Collectors.joining(" "));
        var name = n.getName().asString();
        var typeParameters = n.getTypeParameters();

        var extend = n.getExtendedTypes().stream();
        var implement = n.getImplementedTypes().stream();
        var base = Streams.concat(extend, implement)
            .map(x -> x.toString())
            .collect(Collectors.joining(", "));
        var sb = new StringBuilder();
        sb.append(modifiers);
        sb.append(n.isInterface() ? " interface " : " class ");
        sb.append(name);
        sb.append(getTypeParameters(typeParameters));
//        if(typeParameters.isNonEmpty()){
//            sb.append("<");
//            sb.append(typeParameters.stream()
//                .map(x -> x.getName().asString())
//                .collect(Collectors.joining(", ")));
//            sb.append(">");
//        }

        if(base != ""){
            sb.append(" : ");
            sb.append(base);
        }
//
//        for(var type : typeParameters){
//            var bounds = type.getTypeBound();
//            if(bounds.isNonEmpty()){
//                sb.append(" where ");
//                sb.append(type.getNameAsString());
//                sb.append(" : ");
//                sb.append(bounds.stream()
//                        .map(x -> x.toString())
//                        .collect(Collectors.joining(", ")));
//            }
//        }"
        sb.append(getGenericsConstraints(typeParameters));
        return sb.toString();
    }

    private String getTypeParameters(NodeList<TypeParameter> typeParameters){
        var sb = new StringBuilder();
        if(typeParameters.isNonEmpty()){
            sb.append("<");
            sb.append(typeParameters.stream()
                    .map(x -> x.getName().asString())
                    .collect(Collectors.joining(", ")));
            sb.append(">");
        }
        return sb.toString();
    }
    private String getGenericsConstraints(NodeList<TypeParameter> typeParameters){
        var sb = new StringBuilder();
        for(var type : typeParameters){
            var bounds = type.getTypeBound();
            if(bounds.isNonEmpty()){
                sb.append(" where ");
                sb.append(type.getNameAsString());
                sb.append(" : ");
                sb.append(bounds.stream()
                        .map(x -> x.toString())
                        .collect(Collectors.joining(", ")));
            }
        }
        return sb.toString();
    }
}
