This application watches file/directory changes and automatically commits & pushes to the fluxflex.
This is designed for uses who have several fluxflex projects in 1 directory.
The "watching path" of the application should be set at the parent directory which contains the project directoryies.

- parent dir  <- path to "watch"
    - project1
        - public_html
            - index.html
        - file1
        - file2
          ...
    - project2
      ...
    - projectN
