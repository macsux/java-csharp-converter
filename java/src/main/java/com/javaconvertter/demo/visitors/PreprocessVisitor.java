package com.javaconvertter.demo.visitors;

import com.github.javaparser.ast.*;
import com.github.javaparser.ast.body.AnnotationMemberDeclaration;
import com.github.javaparser.ast.body.ClassOrInterfaceDeclaration;
import com.github.javaparser.ast.body.FieldDeclaration;
import com.github.javaparser.ast.body.MethodDeclaration;
import com.github.javaparser.ast.expr.Name;
import com.github.javaparser.ast.expr.SimpleName;
import com.github.javaparser.ast.type.ClassOrInterfaceType;
import com.github.javaparser.ast.visitor.ModifierVisitor;
import com.github.javaparser.ast.visitor.Visitable;

import java.util.*;
import java.util.stream.Collectors;

public class PreprocessVisitor extends ModifierVisitor<Void> {

    static Set<String> keepGenericTypes = new HashSet<> (Arrays.asList(
            "HashSet",
            "HashMap",
            "ArrayList",
            "List"));
    public List<String> originalImports;
    @Override
    public Visitable visit(CompilationUnit n, Void v) {
        n.removeComment();
        originalImports = n.getImports().stream().map(x -> x.getNameAsString()).collect(Collectors.toList());
        super.visit(n, v);
//        n.addImport("System");
//        n.addImport("System.Linq");
//        n.addImport("System.Collections.Generic");
        return n;
    }
    @Override
    public Visitable visit(Modifier n, Void v) {

        if(n.getKeyword() == Modifier.Keyword.FINAL || n.getKeyword() == Modifier.Keyword.STATIC){
            n.getParentNode().ifPresent(p -> {
                if (p instanceof ClassOrInterfaceDeclaration)
                    n.remove();
            });
        }
        return super.visit(n,v);
    }
    @Override
    public Node visit(ImportDeclaration n, Void v) {
//        if(!n.getNameAsString().startsWith("org.axonframework"))
//            return null;
        if(n.isAsterisk()) {
            return n.setAsterisk(false);
        }

        var segments = Arrays.asList(n.getNameAsString().split("\\."));
        segments = new ArrayList<String>(segments);
        segments.remove(segments.size()-1);
        var newName = String.join(".", segments);
        return n.setName(newName);
    }

    @Override
    public Visitable visit(ClassOrInterfaceType n, Void v) {
//        fixTypeParameter(n.getTypeArguments(), n, n.getNameAsString(), () -> n.removeTypeArguments());
        var genericsInScope = Helper.getGenericsInScope(n);
        n.getTypeArguments().ifPresent(args -> {
            for(var arg : args.stream().collect(Collectors.toList())) {
                    if(arg.isWildcardType()){
                        var extendsType = arg.asWildcardType().getExtendedType();
                        if(extendsType.isPresent() && genericsInScope.contains(extendsType.get().asString())){
                            n.replace(arg, extendsType.get());
                        }
                        if(!keepGenericTypes.contains(n.getName().asString())) {
                            n.remove(arg);

                    }else{
                        n.replace(arg, new ClassOrInterfaceType("Object"));
                    }
                }
            }
            if(n.getTypeArguments().get().size() == 0)
                n.removeTypeArguments();
        });

        var result = super.visit(n, v);
        return result;
    }

    @Override
    public Visitable visit(AnnotationMemberDeclaration n, Void v) {
        n.setModifiers(Modifier.Keyword.PUBLIC);
        return super.visit(n,v);
    }

    @Override
    public Visitable visit(MethodDeclaration n, Void v) {
//        return n.clone().setThrownExceptions(new NodeList<>());
        n.setThrownExceptions(new NodeList<>());
        n.setDefault(false);

        return super.visit(n,v);
    }
//    private Map<String,List<String>> getTypeParameters(NodeList<TypeParameter> parameters){
////        parameters.map(typeParam -> typeParam.stream().map(x -> x.isReferenceType())))
////        if(parameters.isPresent()){
////            parameters.get().stream()
////        }
//        var result = new HashMap<String,List<String>>();
//        for(var parameter : parameters){
//            parameter.getTypeBound()
//        }
//    }
//
//    private void fixTypeParameter(Optional<NodeList<Type>> parameters, Node n, String nodeName, Runnable removeAction){
//        parameters.ifPresent(args -> {
//            for(var arg : args.stream().collect(Collectors.toList())) {
//                if(arg.isWildcardType()){
//                    if(!keepGenericTypes.contains(nodeName)) {
//                        n.remove(arg);
//
//                    }else{
//                        n.replace(arg, new ClassOrInterfaceType("Object"));
//                    }
//                }
//            }
//            if(parameters.get().size() == 0)
//                removeAction.run();
////                n.removeTypeArguments();
//        });
//    }

    @Override
    public Visitable visit(FieldDeclaration n, Void v) {

        if(n.getVariables().getFirst().get().getNameAsString().equals("serialVersionUID"))
            n.remove();
        return super.visit(n,v);
    }

    @Override
    public Visitable visit(SimpleName n, Void arg) {
        if(n.getIdentifier().equals("delegate"))
            n.setIdentifier("__delegate");
        return super.visit(n, arg);
    }
}
