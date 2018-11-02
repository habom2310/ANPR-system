import numpy as np
from collections import Counter

plates = []
plates.append('79D181355')
plates.append('29818135')
plates.append('29D181355')
plates.append('298181355')
plates.append('29D131355')

plates_length = [9, 8, 9, 9, 9]

def get_average_plate_value(plates, plates_length):
    plates_to_be_considered = []
    number_char_on_plate = Counter(plates_length).most_common(1)[0][0]
    for plate in plates:
        if (len(plate) == number_char_on_plate):
            plates_to_be_considered.append(plate)

    temp = ''
    for plate in plates_to_be_considered:
        temp = temp + plate
    
    counter = 0
    final_plate = ''
    for i in range(number_char_on_plate):
        my_list = []
        for i in range(len(plates_to_be_considered)):
            my_list.append(temp[i*number_char_on_plate + counter])
        final_plate = final_plate + str(Counter(my_list).most_common(1)[0][0])
        counter += 1
    return final_plate

        

result_of_plate = get_average_plate_value(plates, plates_length)
print result_of_plate
    