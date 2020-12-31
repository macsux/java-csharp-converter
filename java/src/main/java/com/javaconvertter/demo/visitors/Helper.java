package com.javaconvertter.demo.visitors;

import com.github.javaparser.ast.Node;
import com.github.javaparser.ast.NodeList;
import com.github.javaparser.ast.body.ClassOrInterfaceDeclaration;
import com.github.javaparser.ast.body.MethodDeclaration;
import com.github.javaparser.ast.type.TypeParameter;

import java.util.HashSet;
import java.util.Set;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public class Helper {
    public static Set<String> getGenericsInScope(Node memberNode){
        Set<String> genericruless;
        NodeList<TypeParameter> nodeList = new NodeList();
        if(memberNode instanceof ClassOrInterfaceDeclaration){
            nodeList = ((ClassOrInterfaceDeclaration) memberNode).getTypeParameters();
        }
        else if(memberNode instanceof MethodDeclaration) {
            nodeList = ((MethodDeclaration) memberNode).getTypeParameters();
        }

        var parentGenerics = memberNode.getParentNode().map(Helper::getGenericsInScope).orElse(new HashSet<>()).stream();
        var ownGenerics = nodeList.stream()
                .map(x -> x.getNameAsString());
        var result = Stream.concat(parentGenerics, ownGenerics).collect(Collectors.toSet());
        return result;
    }
}
