//package com.javaconvertter.demo;
//
//import org.springframework.boot.SpringApplication;
//import org.springframework.boot.autoconfigure.SpringBootApplication;
//
//@SpringBootApplication
//public class DemoApplication {
//
//    public static void main(String[] args) {
//        SpringApplication.run(DemoApplication.class, args);
//    }
//
//}



package com.javaconvertter.demo;

import com.github.javaparser.JavaParser;
import com.github.javaparser.ParserConfiguration;
import com.github.javaparser.Range;
import com.github.javaparser.ast.CompilationUnit;
import com.github.javaparser.utils.CodeGenerationUtils;
import com.github.javaparser.utils.Log;
import com.javaconvertter.demo.visitors.*;
import lombok.SneakyThrows;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.nio.file.*;
import java.nio.file.attribute.BasicFileAttributes;
import java.util.HashMap;
import java.util.List;

/**
 * Some code that uses JavaParser.
 */
public class DemoApplication {
    private static final Logger logger = LoggerFactory.getLogger(DemoApplication.class);

    @SneakyThrows
    public static void main(String[] args) {


        var projects = new HashMap<Path, Path>();
        projects.put(
                Path.of("C:\\projects\\AxonFramework\\modelling\\src\\main\\java\\org\\axonframework"),
                Path.of("C:\\projects\\NAxonAuto\\first\\Modelling"));
        projects.put(
                Path.of("C:\\projects\\AxonFramework\\messaging\\src\\main\\java\\org\\axonframework"),
                Path.of("C:\\projects\\NAxonAuto\\first\\Messaging"));


        for(var project : projects.entrySet())
        {
            var sourceRoot = project.getKey();
            var targetRoot = project.getValue();
            var walker = new MyFileVisitor(sourceRoot, targetRoot);
            try {

                Files.walkFileTree(sourceRoot, walker);
            } catch (IOException e) {
                e.printStackTrace();
            }
        }

//        CodeGenerationUtils.packageToPath()

//        var fileName = CodeGenerationUtils.fileInPackageAbsolutePath(root, pkg, file).toAbsolutePath();


    }
    static void processFile2(Path sourceRoot, Path targetRoot, Path fileName) throws IOException {
        var fileNameStr = fileName.getFileName().toString();
        System.out.println(fileNameStr.substring(0, fileNameStr.length()-5));
    }


}