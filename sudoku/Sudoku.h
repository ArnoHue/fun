#pragma once
#include <vector>
#include <math.h>

template<int D> class Sudoku
{

public:

    Sudoku(int _startGrid[D][D])
    {
        solutions = new std::vector<int(*)[D]>();
        dimSqrt = (int)sqrt((double)D);
        for (int x = 0; x < D; x++) {
            for (int y = 0; y < D; y++) {
                squareIdx[x][y] = 
                (x / dimSqrt) * dimSqrt + (y / dimSqrt);
            }
        }

        copyGrid(_startGrid, startGrid);
    }

    virtual ~Sudoku() {
        for (std::vector<int(*)[D]>::iterator it = 
            solutions->begin(); it != solutions->end(); it++) {
            delete[] *it;
        }
        delete solutions;
    }

    std::vector<int(*)[D]>* solve() {
        for (std::vector<int(*)[D]>::iterator it = 
            solutions->begin(); it != solutions->end(); it++) {
            delete[] *it;
        }
        solutions->clear();
        copyGrid(startGrid, grid);
        for (int i = 0; i < D; i++) {
            for (int j = 0; j < D; j++) {
                usedInRow[i][j] = false;
                usedInColumn[i][j] = false;
                usedInSquare[i][j] = false;
            }
        }
        for (int x = 0; x < D; x++) {
            for (int y = 0; y < D; y++) {
                int val = grid[x][y];
                if (val != 0) {
                    usedInRow[x][val - 1] = true;
                    usedInColumn[y][val - 1] = true;
                    usedInSquare[squareIdx[x][y]][val - 1] = true;
                }
            }
        }

        digInto(0, 0);
        return solutions;
    }

private:

    void copyGrid(int src[D][D], int tgt[D][D]) {
        for (int x = 0; x < D; x++) {
            // for compatibility reasons with java we 
            // copy one-dimensional arrays only
            memcpy(tgt[x], src[x], D * sizeof(int));
        }
    };

    void digInto(int x, int y) {
        if (startGrid[x][y] == 0) {
            int square = squareIdx[x][y];

            for (int val = 1; val <= D; val++) {
                int valIdx = val - 1;
                if (usedInRow[x][valIdx] || usedInColumn[y][valIdx] || 
                usedInSquare[square][valIdx]) {
                    continue;
                }
                grid[x][y] = val;
                usedInRow[x][valIdx] = true;
                usedInColumn[y][valIdx] = true;
                usedInSquare[square][valIdx] = true;
                if (x < D - 1) {
                    digInto(x + 1, y);
                }
                else if (y < D - 1) {
                    digInto(0, y + 1);
                }
                else {
                    addSolution();
                }
                grid[x][y] = 0;
                usedInRow[x][valIdx] = false;
                usedInColumn[y][valIdx] = false;
                usedInSquare[square][valIdx] = false;
            }

        }
        else {
            if (x < D - 1) {
                digInto(x + 1, y);
            }
            else if (y < D - 1) {
                digInto(0, y + 1);
            }
            else {
                addSolution();
            }
        }
    };

    void addSolution() {
        int (*solution)[D] = new int[D][D];
        copyGrid(grid, solution);
        solutions->push_back(solution);
    };

    int startGrid[D][D];
    int grid[D][D];
    int dimSqrt;
    std::vector<int(*)[D]>* solutions;
    bool usedInColumn[D][D];
    bool usedInRow[D][D];
    bool usedInSquare[D][D];
    int squareIdx[D][D];

};
