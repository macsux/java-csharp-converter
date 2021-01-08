package com.javaconvertter.demo.visitors;

import com.github.javaparser.JavaParser;
import com.github.javaparser.ParserConfiguration;
import com.github.javaparser.Range;
import com.github.javaparser.ast.CompilationUnit;
import com.javaconvertter.demo.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.file.FileVisitResult;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.SimpleFileVisitor;
import java.nio.file.attribute.BasicFileAttributes;
import java.util.List;

public class MyFileVisitor extends SimpleFileVisitor<Path> {
    private static final Logger logger = LoggerFactory.getLogger(MyFileVisitor.class);
    private Path sourceRoot;
    private Path targetRoot;

    public MyFileVisitor(Path sourceRoot, Path targetRoot) {
        this.sourceRoot = sourceRoot;
        this.targetRoot = targetRoot;
    }

    @Override
    public FileVisitResult visitFile(Path file, BasicFileAttributes attrs) throws IOException {
        var relativePath = sourceRoot.relativize(file);
        try {
//            if(file.getFileName().toString().equals("ReflectionUtils.java"))
            processFile(sourceRoot, targetRoot, relativePath);
        }catch(Exception e){
            logger.error(String.format("ERROR in file %s", relativePath));
            e.printStackTrace();
        }
        return FileVisitResult.CONTINUE;
    }

    static void processFile(Path sourceRoot, Path targetRoot, Path fileName) throws IOException {
//        var file = sourceRoot.resolve(file);
//        var fileName = file;
        var fileNameStr = fileName.toString();
        var fileNameNoExtension = fileNameStr.substring(0, fileNameStr.length()-5);
        var targetFileName = Path.of( fileNameNoExtension + ".cs");

        var sourceFile = sourceRoot.resolve(fileName);

        var sourceCode = Files.readString(sourceFile);

        var parserConfiguration = new ParserConfiguration();
        var parser = new JavaParser(parserConfiguration);
        CompilationUnit cu = parser.parse(sourceCode).getResult().get();

//        var rules = new ArrayList<Rule>();
        var rules =     new RuleBuilder();
        var preprocessor = new PreprocessVisitor();
        cu.accept(preprocessor, null);
        sourceCode = cu.toString();
        Files.writeString(Path.of("c:\\temp\\sample.java"), sourceCode);
        cu = parser.parse(sourceCode).getResult().get();
        cu.accept(new CSharpVisitor(preprocessor.originalImports), rules);

        try {


            var result = rewrite(sourceCode, rules.getRules());

            result = result.replace("->", "=>");

//        System.out.println(result);

//        var resultFile = Path.of("c:\\temp\\sample.cs");
            var resultFile = targetRoot.resolve(targetFileName);
            Files.createDirectories(resultFile.getParent());
            Files.writeString(resultFile, result);
        }catch(RuleOverlapsException e){
            logger.error(e.toString());
        }
    }

    static String rewrite(String input, List<Rule> rules){

        var sb = new StringBuilder();

        var rangeConverter = new RangeConverter(input);
        rules.sort((o1, o2) -> {
            Integer s1 = rangeConverter.getAbsolutePosition(o1.getStart());
            Integer s2 = rangeConverter.getAbsolutePosition(o2.getStart());

            return s1.compareTo(s2);
        });
        //validateRules(rules);
        var lastRuleEnd = 0;
        for(int i=0;i< rules.size();i++)
        {
            var rule = rules.get(i);
            var interval = rangeConverter.getInterval(rule);
            try {
                var srcSeg = input.substring(lastRuleEnd, interval.getStart());

            sb.append(srcSeg);
            }catch(Exception e)
            {
                throw e;
            }
            String replacement;
            if(rule.getSearch() != null){
                var originalBlock = input.substring(interval.getStart(), interval.getStop());
                replacement = originalBlock.replaceAll(rule.getSearch(), rule.getReplacement());
            } else {
                replacement = rule.getReplacement();
            }
            sb.append(replacement);
            lastRuleEnd = interval.getStop();
        }
        if(lastRuleEnd < input.length() - 1)
            sb.append(input.substring(lastRuleEnd));
        return sb.toString();

    }
    static void validateRules(List<Rule> rules){
        for(var i = 0; i < rules.size(); i++){
            var rule = rules.get(i);
            for(var k = i+1; k < rules.size(); k++) {
                var otherRule = rules.get(k);
                var r1 = new Range(rule.getStart(), rule.getStop().right(-1));
                var r2 = new Range(otherRule.getStart(), otherRule.getStop().right(-1));
                if(r1.overlapsWith(r2)){
                    throw new RuleOverlapsException(rule, otherRule);
                }
            }
        }
    }
}
